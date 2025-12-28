#!/usr/bin/env python3

import os
import datetime
import json
import glob

# Default settings for the UI extractor, adapted from your previous settings file.
DEFAULT_SETTINGS = {
    # Default folders to scan. **You may still need to adjust these to match your project.**
    "ui_directories": ["Assets/UI", "Assets/UIToolkit"], 
    
    # Naming convention matches your "Unity_EXTRACTED_scripts" file.
    "output_filename": "Unity_EXTRACTED_ToolkitUI",
    
    # Set to 'false' to match your preference.
    "include_timestamp_in_filename": False,
    
    # Set to 'true' to match your preference.
    "clean_previous_output": True,
    
    # Set to 'true' to match your preference.
    "include_toc": True,
    
    # A clear header for the UI file.
    "header_text": """=================================================
UNITY UI TOOLKIT FILES (.uxml & .uss)
=================================================
This document contains all .uxml and .uss files
extracted from the project for easy review.

Project: {project_name}
Extracted on: {extraction_date}
"""
}

def load_settings():
    """Loads settings from ui_extractor_settings.json or creates it with defaults."""
    settings_path = os.path.join(os.getcwd(), "ui_extractor_settings.json")
    if os.path.exists(settings_path):
        try:
            with open(settings_path, 'r', encoding='utf-8') as f:
                user_settings = json.load(f)
                settings = DEFAULT_SETTINGS.copy()
                settings.update(user_settings)
                return settings
        except json.JSONDecodeError:
            print("Warning: Error reading settings.json. File might be corrupt. Using defaults.")
            return DEFAULT_SETTINGS
    else:
        with open(settings_path, 'w', encoding='utf-8') as f:
            json.dump(DEFAULT_SETTINGS, f, indent=4)
        print(f"Created default settings file: {settings_path}")
        return DEFAULT_SETTINGS

def collect_ui_files(project_path, settings):
    """Finds all .uxml and .uss files in the specified directories."""
    ui_files = []
    ui_directories = settings.get("ui_directories", [])
    extensions_to_include = ['.uxml', '.uss']

    print(f"Scanning for {', '.join(extensions_to_include)} files in: {', '.join(ui_directories)}")

    for directory in ui_directories:
        scan_path = os.path.join(project_path, directory)
        if not os.path.exists(scan_path):
            print(f"Warning: Directory not found, skipping: {scan_path}")
            continue
        
        for root, _, files in os.walk(scan_path):
            for file in files:
                if any(file.lower().endswith(ext) for ext in extensions_to_include):
                    full_path = os.path.join(root, file)
                    relative_path = os.path.relpath(full_path, project_path)
                    ui_files.append({'path': relative_path, 'full_path': full_path})
    
    # Sort alphabetically for consistent output
    return sorted(ui_files, key=lambda x: x['path'])

def create_table_of_contents(ui_files, file_locations):
    """Creates a formatted Table of Contents."""
    toc = ["TABLE OF CONTENTS:", "------------------"]
    if not ui_files:
        toc.append("No UI files were found.")
        return toc

    for ui_file in ui_files:
        line_num = file_locations.get(ui_file['path'], '?')
        toc.append(f"- {ui_file['path']} (Line: {line_num})")
    
    toc.append("\n")
    return toc

def extract_ui():
    """Main function to perform the extraction."""
    project_path = os.getcwd()
    project_name = os.path.basename(project_path)
    settings = load_settings()

    # 1. Clean previous output file if enabled
    if settings.get("clean_previous_output", True):
        filename_pattern = os.path.join(project_path, f"{settings.get('output_filename')}*.txt")
        for file in glob.glob(filename_pattern):
            try:
                os.remove(file)
                print(f"Removed old file: {os.path.basename(file)}")
            except OSError as e:
                print(f"Error removing old file {os.path.basename(file)}: {e}")

    # 2. Find all relevant files
    ui_files = collect_ui_files(project_path, settings)
    if not ui_files:
        print("No .uxml or .uss files found in the specified directories. Nothing to extract.")
        return

    print(f"Found {len(ui_files)} UI files to extract.")

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
    for ui_file in ui_files:
        file_header = f"""
{'='*80}
// FILE: {ui_file['path']}
{'='*80}
"""
        all_content.append(file_header)
        file_locations[ui_file['path']] = current_line + 3 # Line where content starts
        current_line += 4

        try:
            with open(ui_file['full_path'], 'r', encoding='utf-8') as f:
                content_lines = f.read().split('\n')
                all_content.extend(content_lines)
                current_line += len(content_lines)
        except Exception as e:
            error_message = f"// ERROR: Could not read file. {e}"
            all_content.append(error_message)
            current_line += 1

    # 5. Generate and insert the Table of Contents
    if settings.get("include_toc", True):
        toc_content = create_table_of_contents(ui_files, file_locations)
        # Replace the placeholder with the actual TOC
        all_content[toc_placeholder_index] = '\n'.join(toc_content)

    # 6. Determine final filename
    output_filename = settings.get("output_filename", "EXTRACTED_Unity_ToolkitUI")
    if settings.get("include_timestamp_in_filename", False):
        timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
        output_filename = f"{output_filename}_{timestamp}.txt"
    else:
        output_filename = f"{output_filename}.txt"

    # 7. Write the final output file
    output_path = os.path.join(project_path, output_filename)
    try:
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(all_content))
        print(f"\nSuccess! All UI files extracted to: {output_path}")
    except IOError as e:
        print(f"\nError: Could not write to output file. {e}")


if __name__ == "__main__":
    extract_ui()
    input("\nExtraction complete. Press Enter to exit.")