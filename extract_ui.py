#!/usr/bin/env python3

import os
import datetime
import json
import glob
import sys

# =================================================================================
# SCRIPT CONFIGURATION
# =================================================================================

# 1. Determines where the script is running to find the settings file correctly.
#    This ensures it works even if you run it from a different working directory.
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
SETTINGS_FILENAME = "ui_extractor_settings.json"
SETTINGS_PATH = os.path.join(SCRIPT_DIR, SETTINGS_FILENAME)

# 2. Default settings (used if json file is missing or corrupt).
DEFAULT_SETTINGS = {
    # Default folders to scan.
    "ui_directories": [
        "Assets/Scripts/A_ToolkitUI"
    ], 
    
    # File extensions to extract.
    "extensions_to_include": [
        ".uxml", 
        ".uss", 
        ".cs"
    ],

    # Naming convention for the output file.
    "output_filename": "Unity_EXTRACTED_ToolkitUI",
    
    # Settings preferences.
    "include_timestamp_in_filename": False,
    "clean_previous_output": True,
    "include_toc": True,
    
    # Header text for the final document.
    "header_text": """=================================================
UNITY UI TOOLKIT FILES
=================================================
This document contains selected source files (.uxml, .uss, .cs)
extracted from the project for easy review.

Project: {project_name}
Extracted on: {extraction_date}
"""
}

# =================================================================================
# FUNCTIONS
# =================================================================================

def wait_for_any_key():
    """Waits for any key press on Windows, or Enter on Mac/Linux."""
    print("\nPress any key to exit...")
    if os.name == 'nt':
        # Windows-specific 'any key' logic
        import msvcrt
        msvcrt.getch()
    else:
        # Mac/Linux fallback (requires Enter)
        input()

def load_settings():
    """Loads settings from json or creates it with defaults in the script's directory."""
    if os.path.exists(SETTINGS_PATH):
        try:
            with open(SETTINGS_PATH, 'r', encoding='utf-8') as f:
                user_settings = json.load(f)
                # Merge defaults with user settings (ensures new keys exist)
                settings = DEFAULT_SETTINGS.copy()
                settings.update(user_settings)
                print(f"Loaded settings from: {SETTINGS_FILENAME}")
                return settings
        except json.JSONDecodeError as e:
            print(f"\n[!] Warning: Error reading {SETTINGS_FILENAME}.")
            print(f"    Details: {e}")
            print("    The file format is invalid. Using default settings for this run.")
            return DEFAULT_SETTINGS
    else:
        # Create the file if it doesn't exist
        try:
            with open(SETTINGS_PATH, 'w', encoding='utf-8') as f:
                json.dump(DEFAULT_SETTINGS, f, indent=4)
            print(f"Created default settings file: {SETTINGS_FILENAME}")
        except IOError as e:
            print(f"Warning: Could not create settings file. {e}")
            
        return DEFAULT_SETTINGS

def collect_files(base_path, settings):
    """Finds all files in the specified directories matching the extensions."""
    found_files = []
    target_directories = settings.get("ui_directories", [])
    
    # Normalize extensions to be lowercase
    extensions = [ext.lower() for ext in settings.get("extensions_to_include", [])]

    print(f"Scanning for {', '.join(extensions)} files...")
    print(f"Looking in: {', '.join(target_directories)}")

    for directory in target_directories:
        scan_path = os.path.join(base_path, directory)
        
        if not os.path.exists(scan_path):
            print(f"Warning: Directory not found, skipping: {scan_path}")
            continue
        
        for root, _, files in os.walk(scan_path):
            for file in files:
                # Check if file ends with any of the allowed extensions
                if any(file.lower().endswith(ext) for ext in extensions):
                    full_path = os.path.join(root, file)
                    relative_path = os.path.relpath(full_path, base_path)
                    found_files.append({'path': relative_path, 'full_path': full_path})
    
    # Sort alphabetically by path for consistent output
    return sorted(found_files, key=lambda x: x['path'])

def create_table_of_contents(found_files, file_locations):
    """Creates a formatted Table of Contents."""
    toc = ["TABLE OF CONTENTS:", "------------------"]
    if not found_files:
        toc.append("No matching files were found.")
        return toc

    for file_info in found_files:
        line_num = file_locations.get(file_info['path'], '?')
        toc.append(f"- {file_info['path']} (Line: {line_num})")
    
    toc.append("\n")
    return toc

def extract_ui():
    """Main function to perform the extraction."""
    # Ensure we are operating relative to the script's location
    project_path = SCRIPT_DIR
    project_name = os.path.basename(project_path)
    
    settings = load_settings()

    # 1. Clean previous output file if enabled
    if settings.get("clean_previous_output", True):
        filename_pattern = os.path.join(project_path, f"{settings.get('output_filename')}*.txt")
        for file in glob.glob(filename_pattern):
            try:
                os.remove(file)
                print(f"Removed old output file: {os.path.basename(file)}")
            except OSError as e:
                print(f"Error removing old file {os.path.basename(file)}: {e}")

    # 2. Find all relevant files
    found_files = collect_files(project_path, settings)
    if not found_files:
        print("\n[!] No files found matching the criteria.")
        print("    1. Check if the folders exist.")
        print(f"    2. Check {SETTINGS_FILENAME} to ensure paths are correct.")
        return

    print(f"Found {len(found_files)} files to extract.")

    # 3. Prepare the output content
    all_content = []
    file_locations = {} # To store the starting line number for the TOC
    current_line = 1

    # Add header
    header = settings.get("header_text", "").format(
        project_name=project_name,
        extraction_date=datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    )
    all_content.extend(header.split('\n'))
    current_line += len(all_content)

    # Add a placeholder for the TOC
    toc_placeholder_index = len(all_content)
    if settings.get("include_toc", True):
        all_content.append("\n[TOC_PLACEHOLDER]\n")
        current_line += 3

    # 4. Read each file and add its content
    for file_info in found_files:
        file_header = f"""
{'='*80}
// FILE: {file_info['path']}
{'='*80}
"""
        all_content.append(file_header)
        file_locations[file_info['path']] = current_line + 3 # Approximate line
        current_line += 4

        try:
            with open(file_info['full_path'], 'r', encoding='utf-8') as f:
                content_lines = f.read().split('\n')
                all_content.extend(content_lines)
                current_line += len(content_lines)
        except Exception as e:
            error_message = f"// ERROR: Could not read file. {e}"
            all_content.append(error_message)
            current_line += 1

    # 5. Generate and insert the Table of Contents
    if settings.get("include_toc", True):
        toc_content = create_table_of_contents(found_files, file_locations)
        # Replace the placeholder with the actual TOC
        all_content[toc_placeholder_index] = '\n'.join(toc_content)

    # 6. Determine final filename
    base_filename = settings.get("output_filename", "EXTRACTED_Unity_ToolkitUI")
    if settings.get("include_timestamp_in_filename", False):
        timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
        output_filename = f"{base_filename}_{timestamp}.txt"
    else:
        output_filename = f"{base_filename}.txt"

    # 7. Write the final output file
    output_path = os.path.join(project_path, output_filename)
    try:
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(all_content))
        print(f"\nSuccess! All files extracted to: {output_filename}")
    except IOError as e:
        print(f"\nError: Could not write to output file. {e}")


if __name__ == "__main__":
    extract_ui()
    wait_for_any_key()