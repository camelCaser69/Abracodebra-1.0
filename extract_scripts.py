#!/usr/bin/env python3

import os
import datetime
import json
import math
import shutil
import glob

# Default settings
DEFAULT_SETTINGS = {
    "max_chars_per_file": 30000,
    "scripts_directories": ["Assets/Scripts"],  # Changed to list for multiple directories
    "treeBlacklist_directories": [],  # New setting to blacklist directories from tree
    "main_output_filename": "EXTRACTOR_scripts",
    "part_output_filename": "EXTRACTOR_scripts_part",
    "exclude_extensions": [".meta"],
    "include_extensions": [".cs"],
    "clean_previous_files": True,
    "backup_previous_files": False,
    "custom_header_text": "This document contains extracted Unity C# scripts from my project. Do not reply—just confirm storing this in memory. If the full script collection exceeds the character limit, additional parts will follow. Use this to update your understanding of the project until further updates.",
    "backup_directory": "_SCRIPT_EXTRACTOR_backups",
    "include_timestamp_in_filename": False,
    "part_end_text": "This is part {current_part} out of {total_parts} of script collection. {remaining_parts} more parts remain.",
    "last_part_end_text": "This is the final part ({current_part} of {total_parts})."
}

def load_settings():
    """Load settings from settings.json or create it with defaults if it doesn't exist."""
    settings_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "script_extractor_settings.json")
    
    if os.path.exists(settings_path):
        try:
            with open(settings_path, 'r', encoding='utf-8') as f:
                user_settings = json.load(f)
                # Merge with defaults to ensure all settings exist
                settings = DEFAULT_SETTINGS.copy()
                settings.update(user_settings)
                return settings
        except Exception as e:
            print(f"Error loading settings: {str(e)}. Using defaults.")
            return DEFAULT_SETTINGS
    else:
        # Create default settings file
        with open(settings_path, 'w', encoding='utf-8') as f:
            json.dump(DEFAULT_SETTINGS, f, indent=4)
        print(f"Created default settings file at: {settings_path}")
        return DEFAULT_SETTINGS

def generate_tree(startpath, settings, prefix=''):
    """Generate a tree-like representation of the directory structure."""
    tree = []
    
    # Get directory contents and sort them
    try:
        contents = sorted(os.listdir(startpath))
    except Exception:
        return tree
    
    # Filter out excluded files based on extensions
    filtered_contents = []
    for item in contents:
        # Skip hidden files and directories
        if item.startswith('.'):
            continue
        
        # Skip files with excluded extensions
        skip = False
        for ext in settings["exclude_extensions"]:
            if item.endswith(ext):
                skip = True
                break
        
        # Skip blacklisted directories
        path = os.path.join(startpath, item)
        rel_path = os.path.relpath(path, os.path.join(os.getcwd(), "Assets"))
        if os.path.isdir(path):
            for blacklisted in settings.get("treeBlacklist_directories", []):
                if rel_path == blacklisted or rel_path.startswith(blacklisted + os.sep):
                    skip = True
                    break
        
        if not skip:
            filtered_contents.append(item)
    
    # Process the filtered items
    for i, item in enumerate(filtered_contents):
        path = os.path.join(startpath, item)
        is_last = i == len(filtered_contents) - 1
        
        # Format the current item
        if is_last:
            tree.append(f"{prefix}└── {item}")
            ext_prefix = prefix + "    "
        else:
            tree.append(f"{prefix}├── {item}")
            ext_prefix = prefix + "│   "
            
        # Recursively process directories
        if os.path.isdir(path):
            tree.extend(generate_tree(path, settings, ext_prefix))
            
    return tree

def create_header(settings, part_num=None, total_parts=None):
    """Create header section for the output file."""
    header = []
    timestamp = datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    
    # Add custom header text if provided
    if settings.get("custom_header_text"):
        header.append(settings["custom_header_text"])
        header.append("")
    
    header.append("=" * 80)
    header.append(f"UNITY PROJECT SCRIPT EXPORT - {timestamp}")
    if part_num is not None and total_parts is not None:
        header.append(f"PART {part_num} OF {total_parts}")
    header.append("=" * 80)
    header.append("")
    
    return header

def create_directory_tree(assets_path, settings):
    """Create directory tree section."""
    tree_section = []
    
    tree_section.append("DIRECTORY STRUCTURE")
    tree_section.append("-" * 80)
    tree_section.append("Assets")
    
    try:
        tree = generate_tree(assets_path, settings)
        tree_section.extend(tree)
    except Exception as e:
        tree_section.append(f"Error generating directory tree: {str(e)}")
    
    tree_section.append("-" * 80)
    tree_section.append("")
    
    return tree_section

def split_content(all_content, max_chars, tree_section, settings):
    """Split content into multiple parts if needed, preserving whole scripts."""
    parts = []
    
    # Extract scripts section
    header_end = 0
    for i, line in enumerate(all_content):
        if line == "SCRIPT CONTENTS":
            header_end = i
            break
            
    if header_end == 0:
        # Something went wrong, return everything as one part
        return [all_content]
        
    scripts_start = header_end + 3  # Skip the header, separator line, and blank line
    scripts_section = all_content[scripts_start:]
    
    # Check if we need to split at all
    if len('\n'.join(all_content)) <= max_chars:
        return [all_content]
    
    # Find script boundaries and track complete scripts (header + content)
    complete_scripts = []
    current_script = []
    
    i = 0
    while i < len(scripts_section):
        line = scripts_section[i]
        
        # Start of a new script
        if line == "/" * 80:
            # If we already have a script in progress, save it
            if current_script:
                complete_scripts.append(current_script)
                current_script = []
            
            # Add this line (script separator)
            current_script.append(line)
            
            # Add next line (script filename)
            if i+1 < len(scripts_section):
                current_script.append(scripts_section[i+1])
                i += 1
                
            # Add next line (script separator)
            if i+1 < len(scripts_section):
                current_script.append(scripts_section[i+1])
                i += 1
        else:
            # Add to current script
            current_script.append(line)
        
        i += 1
    
    # Add the last script if any
    if current_script:
        complete_scripts.append(current_script)
    
    # Calculate script sizes
    script_sizes = [len('\n'.join(script)) + 1 for script in complete_scripts]  # +1 for newline
    
    # Now create the parts with whole scripts
    pre_scripts_content = all_content[:scripts_start]
    pre_scripts_size = len('\n'.join(pre_scripts_content)) + 1  # +1 for newline
    
    # Determine how many parts we'll need
    total_parts = 1
    current_size = pre_scripts_size
    current_script = 0
    
    while current_script < len(script_sizes):
        if current_size + script_sizes[current_script] > max_chars and current_size > pre_scripts_size:
            total_parts += 1
            current_size = pre_scripts_size  # Reset with header and tree
        current_size += script_sizes[current_script]
        current_script += 1
    
    # Get the part end text templates
    part_end_text_template = settings.get("part_end_text", "This is part {current_part} out of {total_parts}. {remaining_parts} more parts remain.")
    last_part_end_text_template = settings.get("last_part_end_text", "This is the final part ({current_part} of {total_parts}).")
    
    # Now actually split the content
    part_num = 1
    header_for_part = create_header(settings, part_num, total_parts)
    scripts_header = ["SCRIPT CONTENTS", "=" * 80, ""]
    
    current_part = header_for_part + all_content[len(header_for_part):scripts_start] + scripts_header
    current_size = len('\n'.join(current_part)) + 1
    
    for i, script in enumerate(complete_scripts):
        script_size = script_sizes[i]
        
        # See if it fits in current part
        if current_size + script_size > max_chars and current_size > len('\n'.join(header_for_part + all_content[len(header_for_part):scripts_start] + scripts_header)) + 1:
            # Add part end text before finishing this part
            if part_num < total_parts:
                part_end_text = part_end_text_template.format(
                    current_part=part_num, 
                    total_parts=total_parts,
                    remaining_parts=total_parts - part_num
                )
                current_part.append("")
                current_part.append("-" * 80)
                current_part.append(part_end_text)
                current_part.append("-" * 80)
            
            # Finish this part and start a new one
            parts.append(current_part)
            part_num += 1
            header_for_part = create_header(settings, part_num, total_parts)
            current_part = header_for_part + all_content[len(header_for_part):scripts_start] + scripts_header
            current_size = len('\n'.join(current_part)) + 1
        
        # Add script to current part
        current_part.extend(script)
        current_part.append("")  # Add empty line after script
        current_part.append("")  # Add another empty line for separation
        current_size += script_size + 2  # +2 for the two empty lines
    
    # Add final part end text if needed
    if total_parts > 1:
        last_part_end_text = last_part_end_text_template.format(
            current_part=part_num, 
            total_parts=total_parts
        )
        current_part.append("")
        current_part.append("-" * 80)
        current_part.append(last_part_end_text)
        current_part.append("-" * 80)
    
    # Add the last part
    if current_part:
        parts.append(current_part)
    
    return parts

def clean_previous_files(project_path, settings, timestamp):
    """Clean up previous output files if configured to do so."""
    main_filename = settings.get("main_output_filename", "project_scripts")
    part_filename = settings.get("part_output_filename", "project_scripts_part")
    
    main_pattern = os.path.join(project_path, f"{main_filename}*.txt")
    part_pattern = os.path.join(project_path, f"{part_filename}*.txt")
    
    # Find all matching files
    files_to_clean = glob.glob(main_pattern)
    files_to_clean.extend(glob.glob(part_pattern))
    
    if not files_to_clean:
        return
        
    # Create backup directory if needed
    if settings.get("backup_previous_files", True):
        backup_dir = os.path.join(project_path, settings.get("backup_directory", "_script_extractor_backups"))
        backup_timestamp_dir = os.path.join(backup_dir, timestamp)
        
        if not os.path.exists(backup_dir):
            os.makedirs(backup_dir)
        if not os.path.exists(backup_timestamp_dir):
            os.makedirs(backup_timestamp_dir)
    
    # Process each file
    for file_path in files_to_clean:
        if settings.get("backup_previous_files", True):
            # Copy to backup
            filename = os.path.basename(file_path)
            backup_path = os.path.join(backup_timestamp_dir, filename)
            try:
                shutil.copy2(file_path, backup_path)
                print(f"Backed up: {filename} to {backup_timestamp_dir}")
            except Exception as e:
                print(f"Failed to backup {filename}: {str(e)}")
        
        # Remove file
        try:
            os.remove(file_path)
            print(f"Removed previous file: {os.path.basename(file_path)}")
        except Exception as e:
            print(f"Failed to remove {os.path.basename(file_path)}: {str(e)}")

def extract_scripts(project_path):
    """Extract all C# scripts and directory structure from a Unity project."""
    
    # Load settings
    settings = load_settings()
    
    assets_path = os.path.join(project_path, "Assets")
    timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    max_chars = settings["max_chars_per_file"]
    
    # Get filenames from settings
    main_filename = settings.get("main_output_filename", "project_scripts")
    part_filename = settings.get("part_output_filename", "project_scripts_part")
    include_timestamp = settings.get("include_timestamp_in_filename", True)
    
    # Clean previous files if configured to do so
    if settings.get("clean_previous_files", False):
        clean_previous_files(project_path, settings, timestamp)
    
    # Verify that assets path exists
    if not os.path.exists(assets_path):
        print(f"Error: Assets folder not found at {assets_path}")
        return
    
    # Initialize output content
    all_content = []
    
    # Add header (temporary, will be replaced for each part)
    all_content.extend(create_header(settings))
    
    # Add directory tree section
    tree_section = create_directory_tree(assets_path, settings)
    all_content.extend(tree_section)
    
    # Add scripts section
    all_content.append("SCRIPT CONTENTS")
    all_content.append("=" * 80)
    all_content.append("")
    
    # Find and extract all C# scripts from all script directories
    script_directories = settings.get("scripts_directories", ["Assets/Scripts"])
    
    # Collect all script files
    script_files = []
    missing_dirs = []
    
    for scripts_dir in script_directories:
        scripts_path = os.path.join(project_path, scripts_dir)
        
        if not os.path.exists(scripts_path):
            missing_dirs.append(scripts_dir)
            continue
            
        for root, _, files in os.walk(scripts_path):
            for file in files:
                for ext in settings["include_extensions"]:
                    if file.endswith(ext):
                        script_files.append(os.path.join(root, file))
                        break
    
    if missing_dirs:
        all_content.append(f"Note: The following script directories were not found: {', '.join(missing_dirs)}")
    
    if not script_files:
        all_content.append("Note: No script files found in any of the specified directories.")
    else:
        # Sort script files by relative path for better organization
        script_files.sort(key=lambda x: os.path.relpath(x, project_path))
        
        # Extract and format each script
        for script_path in script_files:
            rel_path = os.path.relpath(script_path, project_path)
            
            all_content.append("/" * 80)
            all_content.append(f"// FILE: {rel_path}")
            all_content.append("/" * 80)
            
            try:
                with open(script_path, 'r', encoding='utf-8') as f:
                    script_content = f.read()
                    all_content.append(script_content)
            except Exception as e:
                all_content.append(f"// Error reading file: {str(e)}")
                
            all_content.append("")
            all_content.append("")
    
    # Generate filenames with or without timestamp
    if include_timestamp:
        main_output_filename = f"{main_filename}_{timestamp}.txt"
        part_output_filename_template = f"{part_filename}_{timestamp}_{{}}of{{}}.txt"
    else:
        main_output_filename = f"{main_filename}.txt"
        part_output_filename_template = f"{part_filename}_{{}}of{{}}.txt"
    
    # Always write the full file first
    main_output_path = os.path.join(project_path, main_output_filename)
    try:
        with open(main_output_path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(all_content))
        print(f"Script extraction complete. Full output saved to: {main_output_path}")
    except Exception as e:
        print(f"Error writing full output file: {str(e)}")
    
    # Split content if needed
    if len('\n'.join(all_content)) > max_chars:
        content_parts = split_content(all_content, max_chars, tree_section, settings)
        
        # Write the split files
        for i, part_content in enumerate(content_parts):
            part_output_path = os.path.join(
                project_path, 
                part_output_filename_template.format(i+1, len(content_parts))
            )
            
            try:
                with open(part_output_path, 'w', encoding='utf-8') as f:
                    f.write('\n'.join(part_content))
                print(f"Split part {i+1} of {len(content_parts)} saved to: {part_output_path}")
            except Exception as e:
                print(f"Error writing part file: {str(e)}")

if __name__ == "__main__":
    # Use the current directory as the project path
    current_dir = os.getcwd()
    extract_scripts(current_dir)
    print("Done. Place this script in your Unity project root folder and run it.")