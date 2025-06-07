#!/usr/bin/env python3

import os
import datetime
import json
import re
import shutil
import glob

# Default settings with all compression options
DEFAULT_SETTINGS = {
    "max_chars_per_file": 10000000,  # 10 million chars for single file output
    "scripts_directories": ["Assets/Scripts"],
    "treeBlacklist_directories": [],
    "main_output_filename": "EXTRACTOR_scripts",
    "part_output_filename": "EXTRACTOR_scripts_part",
    "exclude_extensions": [".meta"],
    "include_extensions": [".cs"],
    "clean_previous_files": True,
    "backup_previous_files": False,
    "backup_directory": "_SCRIPT_EXTRACTOR_backups",
    "include_timestamp_in_filename": False,
    
    # Compression settings
    "compression_enabled": True,
    "remove_empty_lines": True,
    "remove_comments": True,  # Remove all comments including TODO/FIXME
    "remove_xml_docs": True,  # Remove /// XML documentation
    "remove_using_statements": True,  # Remove common using statements
    "remove_regions": True,  # Remove #region/#endregion
    "remove_attributes": True,  # Remove [Header], [Tooltip], etc.
    "trim_whitespace": True,  # Trim trailing whitespace
    "compress_braces": True,  # Put opening braces on same line
    "compress_method_signatures": True,  # Single-line method signatures
    "compress_namespaces": True,  # Namespace declaration on same line as {
    "shorten_modifiers": True,  # Shorten access modifiers
    "extreme_compression": False,  # Maximum compression mode
    
    # Common using statements to remove (if remove_using_statements is True)
    "common_usings": [
        "using System;",
        "using System.Collections;",
        "using System.Collections.Generic;",
        "using UnityEngine;",
        "using UnityEngine.UI;",
        "using System.Linq;",
        "using UnityEngine.Events;"
    ],
    
    # Output format
    "output_format": "compressed",  # "compressed" or "readable"
    "include_toc": True,  # Include table of contents
    "include_compression_notice": True,  # Add notice about formatting
    "compact_toc": True,  # Use more compact TOC format
    
    # Header text templates with stats placeholders
    "compressed_header_text": """UNITY PROJECT SCRIPTS - COMPRESSED FORMAT
Compression Stats: {original_size:,} → {compressed_size:,} chars ({saved_percent:.1f}% reduction)
Estimated tokens saved: ~{tokens_saved:,}

This file is optimized for AI/LLM token efficiency. The following compression is applied:
- Removed: excess whitespace, common using statements, XML docs, attributes
- Preserved: code structure, important comments, logic flow

IMPORTANT FOR AI: When editing/creating scripts from this reference:
1. Re-add standard Unity using statements (System, UnityEngine, etc.)
2. Format with proper indentation and spacing
3. Add back regions and documentation as needed
4. Ensure proper C# code style

Common using statements (add these back when needed):
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Events;

TABLE OF CONTENTS:""",
    
    "readable_header_text": "This document contains extracted Unity C# scripts from my project."
}

def load_settings():
    """Load settings from settings.json or create it with defaults if it doesn't exist."""
    settings_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "script_extractor_settings.json")
    
    if os.path.exists(settings_path):
        try:
            with open(settings_path, 'r', encoding='utf-8') as f:
                user_settings = json.load(f)
                settings = DEFAULT_SETTINGS.copy()
                settings.update(user_settings)
                return settings
        except Exception as e:
            print(f"Error loading settings: {str(e)}. Using defaults.")
            return DEFAULT_SETTINGS
    else:
        with open(settings_path, 'w', encoding='utf-8') as f:
            json.dump(DEFAULT_SETTINGS, f, indent=4)
        print(f"Created default settings file at: {settings_path}")
        return DEFAULT_SETTINGS

def compress_script_content(content, settings):
    """Apply compression settings to script content."""
    if not settings.get("compression_enabled", True):
        return content
    
    lines = content.split('\n')
    compressed_lines = []
    in_multiline_comment = False
    skip_next_brace = False
    collecting_signature = False
    signature_parts = []
    
    i = 0
    while i < len(lines):
        line = lines[i]
        original_line = line
        stripped_line = line.strip()
        
        # Handle multiline comments
        if '/*' in line:
            in_multiline_comment = True
        if in_multiline_comment:
            if settings.get("remove_comments", False):
                if '*/' in line:
                    in_multiline_comment = False
                i += 1
                continue
            else:
                compressed_lines.append(line)
                if '*/' in line:
                    in_multiline_comment = False
                i += 1
                continue
        
        # Skip XML documentation
        if settings.get("remove_xml_docs", True) and stripped_line.startswith('///'):
            i += 1
            continue
        
        # Skip attributes
        if settings.get("remove_attributes", True) and stripped_line.startswith('[') and stripped_line.endswith(']'):
            i += 1
            continue
        
        # Skip single-line comments
        if settings.get("remove_comments", False) and stripped_line.startswith('//'):
            # In full removal mode, skip ALL comments
            i += 1
            continue
        
        # Skip regions
        if settings.get("remove_regions", True):
            if stripped_line.startswith('#region') or stripped_line.startswith('#endregion'):
                i += 1
                continue
        
        # Skip common using statements
        if settings.get("remove_using_statements", True):
            if any(stripped_line == using for using in settings.get("common_usings", [])):
                i += 1
                continue
        
        # Handle method signature compression
        if settings.get("compress_method_signatures", True) and not collecting_signature:
            # Detect start of method signature
            if ('(' in stripped_line and ')' not in stripped_line and 
                (any(mod in stripped_line for mod in ['public', 'private', 'protected', 'internal', 'static', 'virtual', 'override', 'abstract']) or
                 any(type in stripped_line for type in ['void', 'string', 'int', 'float', 'bool', 'double', 'decimal']))):
                collecting_signature = True
                signature_parts = [line.rstrip()]
                i += 1
                continue
        
        # Continue collecting signature
        if collecting_signature:
            signature_parts.append(stripped_line)
            if ')' in stripped_line:
                collecting_signature = False
                # Compress into single line
                compressed_signature = ' '.join(part.strip() for part in signature_parts)
                # Apply modifier shortening if enabled
                if settings.get("shorten_modifiers", True):
                    compressed_signature = shorten_modifiers(compressed_signature, settings)
                compressed_lines.append(compressed_signature)
                signature_parts = []
            i += 1
            continue
        
        # Compress namespace declarations
        if settings.get("compress_namespaces", True) and stripped_line.startswith('namespace'):
            if i + 1 < len(lines) and lines[i + 1].strip() == '{':
                compressed_lines.append(line.rstrip() + ' {')
                i += 2  # Skip both namespace line and opening brace
                continue
        
        # Skip empty lines if previous line was also empty
        if settings.get("remove_empty_lines", True):
            if not stripped_line and compressed_lines and not compressed_lines[-1].strip():
                i += 1
                continue
        
        # Shorten modifiers
        if settings.get("shorten_modifiers", True):
            line = shorten_modifiers(line, settings)
        
        # Trim whitespace
        if settings.get("trim_whitespace", True):
            line = line.rstrip()
        
        # Extreme compression - remove all indentation
        if settings.get("extreme_compression", False):
            line = line.lstrip()
        
        # Compress braces
        if settings.get("compress_braces", True) and stripped_line == '{' and compressed_lines:
            last_line = compressed_lines[-1]
            if last_line.strip() and not last_line.strip().endswith('{'):
                compressed_lines[-1] = last_line + ' {'
                i += 1
                continue
        
        compressed_lines.append(line)
        i += 1
    
    # Remove trailing empty lines
    while compressed_lines and not compressed_lines[-1].strip():
        compressed_lines.pop()
    
    return '\n'.join(compressed_lines)

def shorten_modifiers(line, settings):
    """Shorten access modifiers and keywords."""
    if not settings.get("shorten_modifiers", True):
        return line
    
    # Only shorten at the beginning of the line (with proper word boundaries)
    replacements = [
        (r'\bprivate\s+', ''),  # private is default in C#
        (r'\bprotected\s+', 'prot '),
        (r'\bpublic\s+', 'pub '),
        (r'\binternal\s+', 'int '),
        (r'\bstatic\s+', 'stat '),
        (r'\bvirtual\s+', 'virt '),
        (r'\babstract\s+', 'abs '),
        (r'\boverride\s+', 'ovr '),
        (r'\breadonly\s+', 'ro '),
        (r'\bconst\s+', 'c '),
    ]
    
    # Apply extreme compression replacements if enabled
    if settings.get("extreme_compression", False):
        extreme_replacements = [
            (r'\bclass\s+', 'cls '),
            (r'\binterface\s+', 'ifc '),
            (r'\breturn\s+', 'ret '),
            (r'\bnamespace\s+', 'ns '),
            (r'\bfunction\s+', 'fn '),
        ]
        replacements.extend(extreme_replacements)
    
    for pattern, replacement in replacements:
        line = re.sub(pattern, replacement, line)
    
    return line

def calculate_compression_stats(original, compressed):
    """Calculate compression statistics."""
    original_size = len(original)
    compressed_size = len(compressed)
    saved = original_size - compressed_size
    percentage = (saved / original_size * 100) if original_size > 0 else 0
    tokens_saved = saved // 4  # Rough estimate: 1 token ≈ 4 chars
    return {
        'original': original_size,
        'compressed': compressed_size,
        'saved': saved,
        'percentage': percentage,
        'tokens_saved': tokens_saved
    }

def collect_script_files(project_path, settings):
    """Collect all script files and their information."""
    script_directories = settings.get("scripts_directories", ["Assets/Scripts"])
    script_files = []
    
    for scripts_dir in script_directories:
        scripts_path = os.path.join(project_path, scripts_dir)
        
        if not os.path.exists(scripts_path):
            continue
            
        for root, _, files in os.walk(scripts_path):
            for file in files:
                for ext in settings["include_extensions"]:
                    if file.endswith(ext):
                        full_path = os.path.join(root, file)
                        rel_path = os.path.relpath(full_path, project_path)
                        
                        # Extract namespace and main class if possible (quick scan)
                        namespace = "Unknown"
                        main_class = os.path.splitext(file)[0]
                        
                        try:
                            with open(full_path, 'r', encoding='utf-8') as f:
                                content_preview = f.read(2000)  # Read first 2KB
                                
                                # Try to find namespace
                                ns_match = re.search(r'namespace\s+(\S+)', content_preview)
                                if ns_match:
                                    namespace = ns_match.group(1)
                                
                                # Try to find main public class
                                class_match = re.search(r'public\s+(?:abstract\s+)?(?:class|interface|struct)\s+(\w+)', content_preview)
                                if class_match:
                                    main_class = class_match.group(1)
                        except:
                            pass
                        
                        script_files.append({
                            'full_path': full_path,
                            'rel_path': rel_path,
                            'filename': file,
                            'namespace': namespace,
                            'main_class': main_class
                        })
                        break
    
    # Sort by namespace, then by path
    return sorted(script_files, key=lambda x: (x['namespace'], x['rel_path']))

def create_table_of_contents(script_files, script_locations, settings):
    """Create a compact table of contents."""
    toc = []
    current_namespace = None
    
    compact_mode = settings.get("compact_toc", True)
    
    for script in script_files:
        if script['namespace'] != current_namespace:
            current_namespace = script['namespace']
            toc.append(f"\n[{current_namespace}]")
        
        location = script_locations.get(script['rel_path'], {})
        line_info = f"L{location['line_num']}" if location else "L?"
        
        # Shortened path (remove Assets/Scripts/ prefix if present)
        short_path = script['rel_path']
        for prefix in ["Assets/Scripts/", "Assets/", "Scripts/"]:
            if short_path.startswith(prefix):
                short_path = short_path[len(prefix):]
                break
        
        if compact_mode:
            # Remove filename from path if it matches the class name
            dir_path = os.path.dirname(short_path)
            if dir_path:
                # Compact format: MainClass (shortened/path/) L###
                toc.append(f"  {script['main_class']} ({dir_path}/) {line_info}")
            else:
                # If no directory, just show class name
                toc.append(f"  {script['main_class']} {line_info}")
        else:
            # Original format: MainClass (shortened/path/file.cs) - L###
            toc.append(f"  {script['main_class']} ({short_path}) - {line_info}")
    
    return toc

def extract_scripts(project_path):
    """Extract all C# scripts with compression optimization."""
    settings = load_settings()
    
    assets_path = os.path.join(project_path, "Assets")
    timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    max_chars = settings["max_chars_per_file"]
    
    main_filename = settings.get("main_output_filename", "project_scripts")
    part_filename = settings.get("part_output_filename", "project_scripts_part")
    include_timestamp = settings.get("include_timestamp_in_filename", True)
    
    if settings.get("clean_previous_files", False):
        clean_previous_files(project_path, settings, timestamp)
    
    if not os.path.exists(assets_path):
        print(f"Error: Assets folder not found at {assets_path}")
        return
    
    # Collect script files
    script_files = collect_script_files(project_path, settings)
    
    if not script_files:
        print("No script files found.")
        return
    
    # Initialize content
    all_content = []
    
    # Track locations for TOC
    script_locations = {}
    current_line = 1  # Start at line 1
    
    # Add placeholder for header (will be replaced with stats)
    header_placeholder_lines = 20  # Reserve lines for header
    all_content.extend([''] * header_placeholder_lines)
    current_line += header_placeholder_lines
    
    # Add placeholder for TOC if enabled
    toc_start_line = len(all_content)
    if settings.get("include_toc", True):
        all_content.append("[TOC PLACEHOLDER]")
        all_content.append("")
        current_line += 2
    
    # Add section header
    all_content.append("=" * 80)
    all_content.append("SCRIPTS")
    all_content.append("=" * 80)
    all_content.append("")
    current_line += 4
    
    # Process scripts
    total_original_size = 0
    total_compressed_size = 0
    
    for script_info in script_files:
        script_path = script_info['full_path']
        rel_path = script_info['rel_path']
        
        # Record location
        script_locations[rel_path] = {
            'line_num': current_line + 2,  # Account for header lines
            'part_num': None
        }
        
        # Add script header (compressed format)
        all_content.append("/" * 60)
        all_content.append(f"// {rel_path}")
        all_content.append("")
        
        current_line += 3
        
        try:
            with open(script_path, 'r', encoding='utf-8') as f:
                original_content = f.read()
                
            # Apply compression
            compressed_content = compress_script_content(original_content, settings)
            
            # Track compression stats
            stats = calculate_compression_stats(original_content, compressed_content)
            total_original_size += stats['original']
            total_compressed_size += stats['compressed']
            
            # Add content
            content_lines = compressed_content.split('\n')
            all_content.extend(content_lines)
            current_line += len(content_lines)
            
        except Exception as e:
            all_content.append(f"// Error reading file: {str(e)}")
            current_line += 1
        
        all_content.append("")
        current_line += 1
    
    # Calculate final stats
    total_stats = calculate_compression_stats(
        'x' * total_original_size,  # Dummy string for calculation
        'x' * total_compressed_size
    )
    
    # Generate TOC and insert it
    if settings.get("include_toc", True):
        toc = create_table_of_contents(script_files, script_locations, settings)
        # Replace placeholder
        all_content[toc_start_line:toc_start_line+1] = toc
    
    # Generate header with stats
    if settings.get("compression_enabled", True) and settings.get("include_compression_notice", True):
        header_text = settings.get("compressed_header_text", "").format(
            original_size=total_original_size,
            compressed_size=total_compressed_size,
            saved_percent=total_stats['percentage'],
            tokens_saved=total_stats['tokens_saved']
        )
    else:
        header_text = settings.get("readable_header_text", "")
    
    # Replace header placeholder
    header_lines = header_text.split('\n')
    # Pad or trim to fit the reserved space
    if len(header_lines) < header_placeholder_lines:
        header_lines.extend([''] * (header_placeholder_lines - len(header_lines)))
    else:
        header_lines = header_lines[:header_placeholder_lines]
    
    all_content[0:header_placeholder_lines] = header_lines
    
    # Generate filenames
    if include_timestamp:
        main_output_filename = f"{main_filename}_{timestamp}.txt"
    else:
        main_output_filename = f"{main_filename}.txt"
    
    # Write output
    main_output_path = os.path.join(project_path, main_output_filename)
    
    try:
        with open(main_output_path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(all_content))
        
        print(f"Script extraction complete: {main_output_path}")
        
        if settings.get("compression_enabled", True):
            print(f"\nCompression stats:")
            print(f"  Original: {total_original_size:,} chars")
            print(f"  Compressed: {total_compressed_size:,} chars")
            print(f"  Saved: {total_stats['saved']:,} chars ({total_stats['percentage']:.1f}%)")
            print(f"  Estimated tokens saved: ~{total_stats['tokens_saved']:,}")
        
    except Exception as e:
        print(f"Error writing output: {str(e)}")

def clean_previous_files(project_path, settings, timestamp):
    """Clean up previous output files."""
    main_filename = settings.get("main_output_filename", "project_scripts")
    part_filename = settings.get("part_output_filename", "project_scripts_part")
    
    main_pattern = os.path.join(project_path, f"{main_filename}*.txt")
    part_pattern = os.path.join(project_path, f"{part_filename}*.txt")
    
    files_to_clean = glob.glob(main_pattern)
    files_to_clean.extend(glob.glob(part_pattern))
    
    if not files_to_clean:
        return
    
    if settings.get("backup_previous_files", True):
        backup_dir = os.path.join(project_path, settings.get("backup_directory", "_script_extractor_backups"))
        backup_timestamp_dir = os.path.join(backup_dir, timestamp)
        
        if not os.path.exists(backup_dir):
            os.makedirs(backup_dir)
        if not os.path.exists(backup_timestamp_dir):
            os.makedirs(backup_timestamp_dir)
    
    for file_path in files_to_clean:
        if settings.get("backup_previous_files", True):
            filename = os.path.basename(file_path)
            backup_path = os.path.join(backup_timestamp_dir, filename)
            try:
                shutil.copy2(file_path, backup_path)
                print(f"Backed up: {filename}")
            except Exception as e:
                print(f"Failed to backup {filename}: {str(e)}")
        
        try:
            os.remove(file_path)
            print(f"Removed: {os.path.basename(file_path)}")
        except Exception as e:
            print(f"Failed to remove {os.path.basename(file_path)}: {str(e)}")

if __name__ == "__main__":
    current_dir = os.getcwd()
    extract_scripts(current_dir)
    print("Done.")