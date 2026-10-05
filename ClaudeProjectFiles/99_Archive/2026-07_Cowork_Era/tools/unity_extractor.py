#!/usr/bin/env python3
"""
===============================================================================
UNITY ULTIMATE EXTRACTOR
===============================================================================
A unified tool for extracting Unity project files for LLM context.

Combines script extraction (with compression) and UI file extraction into
a single, profile-based tool optimized for AI/LLM token efficiency.

Usage:
    python unity_extractor.py                  # Run all enabled profiles
    python unity_extractor.py --profile scripts  # Run only scripts profile
    python unity_extractor.py --profile ui       # Run only UI profile
    python unity_extractor.py --list             # List available profiles
    python unity_extractor.py --help             # Show help
===============================================================================
"""

import os
import sys
import datetime
import json
import re
import shutil
import glob
import argparse

# =============================================================================
# CONFIGURATION
# =============================================================================

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
SETTINGS_FILENAME = "unity_extractor_settings.json"
SETTINGS_PATH = os.path.join(SCRIPT_DIR, SETTINGS_FILENAME)

# Default settings with all options
DEFAULT_SETTINGS = {
    # ==========================================================================
    # GLOBAL SETTINGS
    # ==========================================================================
    "global": {
        "clean_previous_files": True,
        "backup_previous_files": False,
        "backup_directory": "_extractor_backups",
        "include_timestamp_in_filename": False,
        "max_chars_per_file": 10000000,
        "show_compression_stats": True
    },
    
    # ==========================================================================
    # EXTRACTION PROFILES
    # ==========================================================================
    "profiles": {
        # ----------------------------------------------------------------------
        # SCRIPTS PROFILE - C# files with compression
        # ----------------------------------------------------------------------
        "scripts": {
            "enabled": True,
            "description": "C# scripts with token-efficient compression",
            
            # Directories to scan (relative to project root)
            "directories": [
                "Assets/Scripts",
                "Assets/Editor"
            ],
            
            # Blacklisted directories (won't be scanned)
            "blacklist_directories": [],
            
            # File extensions
            "include_extensions": [".cs"],
            "exclude_extensions": [".meta"],
            
            # Output settings
            "output_filename": "Unity_EXTRACTED_scripts",
            "part_output_filename": "Unity_EXTRACTED_scripts_part",
            
            # Compression settings (only apply to code files)
            "compression": {
                "enabled": True,
                "remove_empty_lines": True,
                "remove_comments": True,
                "remove_xml_docs": True,
                "remove_using_statements": True,
                "remove_regions": True,
                "remove_attributes": True,
                "trim_whitespace": True,
                "compress_braces": True,
                "compress_method_signatures": True,
                "compress_namespaces": True,
                "shorten_modifiers": True,
                "extreme_compression": True,
                
                # Common using statements to remove
                "common_usings": [
                    "using System;",
                    "using System.Collections;",
                    "using System.Collections.Generic;",
                    "using UnityEngine;",
                    "using UnityEngine.UI;",
                    "using System.Linq;",
                    "using UnityEngine.Events;"
                ]
            },
            
            # Table of contents
            "include_toc": True,
            "compact_toc": True,
            
            # Header template (supports placeholders)
            "header_text": """UNITY PROJECT SCRIPTS - COMPRESSED FORMAT
Compression Stats: {original_size:,} → {compressed_size:,} chars ({saved_percent:.1f}% reduction)
Estimated tokens saved: ~{tokens_saved:,}

This document contains extracted Unity 6 (version 6000.0.39f1) C# scripts from my Unity game project.

When you're tasked with editing: return each changed method in full; if edits touch ≤3 methods list those blocks only, else output the entire script—always copy-paste ready.

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

TABLE OF CONTENTS:"""
        },
        
        # ----------------------------------------------------------------------
        # UI PROFILE - UI Toolkit files (no compression)
        # ----------------------------------------------------------------------
        "ui": {
            "enabled": True,
            "description": "UI Toolkit files (.uxml, .uss, .cs) - no compression",
            
            # Directories to scan
            "directories": [
                "Assets/Scripts/A_ToolkitUI"
            ],
            
            # Blacklisted directories
            "blacklist_directories": [],
            
            # File extensions
            "include_extensions": [".uxml", ".uss", ".cs"],
            "exclude_extensions": [".meta"],
            
            # Output settings
            "output_filename": "Unity_EXTRACTED_ToolkitUI",
            
            # Compression disabled for UI files (preserve formatting)
            "compression": {
                "enabled": False
            },
            
            # Table of contents
            "include_toc": True,
            "compact_toc": False,
            
            # Header template
            "header_text": """=================================================
UNITY UI TOOLKIT FILES
=================================================
This document contains selected source files (.uxml, .uss, .cs)
extracted from the project for easy review.

Project: {project_name}
Extracted on: {extraction_date}
"""
        },
        
        # ----------------------------------------------------------------------
        # CUSTOM PROFILE TEMPLATE (disabled by default)
        # ----------------------------------------------------------------------
        "custom": {
            "enabled": False,
            "description": "Custom extraction profile - configure as needed",
            
            "directories": [],
            "blacklist_directories": [],
            "include_extensions": [],
            "exclude_extensions": [".meta"],
            
            "output_filename": "Unity_EXTRACTED_custom",
            
            "compression": {
                "enabled": False
            },
            
            "include_toc": True,
            "compact_toc": False,
            
            "header_text": "Custom extraction output\n"
        }
    }
}


# =============================================================================
# UTILITY FUNCTIONS
# =============================================================================

def wait_for_any_key():
    """Waits for any key press on Windows, or Enter on Mac/Linux."""
    print("\nPress any key to exit...")
    if os.name == 'nt':
        import msvcrt
        msvcrt.getch()
    else:
        input()


def load_settings():
    """Load settings from JSON or create default file."""
    if os.path.exists(SETTINGS_PATH):
        try:
            with open(SETTINGS_PATH, 'r', encoding='utf-8') as f:
                user_settings = json.load(f)
                # Deep merge with defaults
                settings = deep_merge(DEFAULT_SETTINGS.copy(), user_settings)
                print(f"✓ Loaded settings from: {SETTINGS_FILENAME}")
                return settings
        except json.JSONDecodeError as e:
            print(f"\n⚠ Warning: Error reading {SETTINGS_FILENAME}")
            print(f"  Details: {e}")
            print("  Using default settings for this run.")
            return DEFAULT_SETTINGS
    else:
        try:
            with open(SETTINGS_PATH, 'w', encoding='utf-8') as f:
                json.dump(DEFAULT_SETTINGS, f, indent=4)
            print(f"✓ Created default settings file: {SETTINGS_FILENAME}")
        except IOError as e:
            print(f"⚠ Warning: Could not create settings file. {e}")
        return DEFAULT_SETTINGS


def deep_merge(base, override):
    """Deep merge two dictionaries."""
    result = base.copy()
    for key, value in override.items():
        if key in result and isinstance(result[key], dict) and isinstance(value, dict):
            result[key] = deep_merge(result[key], value)
        else:
            result[key] = value
    return result


def format_size(size):
    """Format byte size to human readable string."""
    for unit in ['', 'K', 'M', 'G']:
        if abs(size) < 1024:
            return f"{size:.1f}{unit}"
        size /= 1024
    return f"{size:.1f}T"


# =============================================================================
# COMPRESSION FUNCTIONS
# =============================================================================

def compress_content(content, compression_settings, file_extension=None):
    """Route to appropriate compressor based on file type."""
    if not compression_settings.get("enabled", False):
        return content
    
    # Route to specialized compressors
    if file_extension == '.uss':
        return compress_uss_content(content, compression_settings)
    elif file_extension == '.uxml':
        return compress_uxml_content(content, compression_settings)
    elif file_extension == '.cs':
        return compress_csharp_content(content, compression_settings)
    else:
        return content


def compress_uss_content(content, compression_settings):
    """Compress USS (Unity Style Sheets) - CSS-like format.
    
    Light compression that preserves readability:
    - Removes /* */ comments
    - Consolidates empty lines
    - Optionally removes unnecessary whitespace
    """
    lines = content.split('\n')
    compressed_lines = []
    in_multiline_comment = False
    prev_was_empty = False
    
    for line in lines:
        # Handle multiline comments
        if '/*' in line and '*/' in line:
            # Single-line comment, remove it
            line = re.sub(r'/\*.*?\*/', '', line)
        elif '/*' in line:
            in_multiline_comment = True
            line = line[:line.index('/*')]
        elif '*/' in line:
            in_multiline_comment = False
            line = line[line.index('*/') + 2:]
        elif in_multiline_comment:
            continue
        
        stripped = line.strip()
        
        # Skip empty lines if previous was also empty
        if not stripped:
            if not prev_was_empty:
                compressed_lines.append('')
                prev_was_empty = True
            continue
        
        prev_was_empty = False
        
        # Light whitespace compression: collapse multiple spaces but keep structure
        if compression_settings.get("compress_whitespace", False):
            line = re.sub(r'  +', ' ', line)
            line = line.strip()
        
        compressed_lines.append(line)
    
    # Remove trailing empty lines
    while compressed_lines and not compressed_lines[-1].strip():
        compressed_lines.pop()
    
    return '\n'.join(compressed_lines)


def compress_uxml_content(content, compression_settings):
    """Compress UXML (Unity XML UI) files.
    
    Light compression that preserves readability:
    - Removes <!-- --> comments
    - Consolidates empty lines
    - Preserves structure and indentation
    """
    lines = content.split('\n')
    compressed_lines = []
    in_multiline_comment = False
    prev_was_empty = False
    
    for line in lines:
        original_line = line
        
        # Handle XML comments
        if '<!--' in line and '-->' in line:
            # Single-line comment - optionally remove
            if compression_settings.get("remove_comments", False):
                line = re.sub(r'<!--.*?-->', '', line)
        elif '<!--' in line:
            in_multiline_comment = True
            if compression_settings.get("remove_comments", False):
                line = line[:line.index('<!--')]
        elif '-->' in line:
            in_multiline_comment = False
            if compression_settings.get("remove_comments", False):
                line = line[line.index('-->') + 3:]
            else:
                compressed_lines.append(original_line)
            continue
        elif in_multiline_comment:
            if not compression_settings.get("remove_comments", False):
                compressed_lines.append(line)
            continue
        
        stripped = line.strip()
        
        # Skip empty lines if previous was also empty
        if not stripped:
            if not prev_was_empty:
                compressed_lines.append('')
                prev_was_empty = True
            continue
        
        prev_was_empty = False
        compressed_lines.append(line.rstrip())
    
    # Remove trailing empty lines
    while compressed_lines and not compressed_lines[-1].strip():
        compressed_lines.pop()
    
    return '\n'.join(compressed_lines)


def compress_csharp_content(content, compression_settings):
    """Apply compression settings to C# file content."""
    lines = content.split('\n')
    compressed_lines = []
    in_multiline_comment = False
    collecting_signature = False
    signature_parts = []
    
    i = 0
    while i < len(lines):
        line = lines[i]
        stripped_line = line.strip()
        
        # Handle multiline comments
        if '/*' in line:
            in_multiline_comment = True
        if in_multiline_comment:
            if compression_settings.get("remove_comments", False):
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
        if compression_settings.get("remove_xml_docs", True) and stripped_line.startswith('///'):
            i += 1
            continue
        
        # Skip attributes (single-line)
        if compression_settings.get("remove_attributes", True):
            if stripped_line.startswith('[') and stripped_line.endswith(']'):
                # But preserve important attributes
                preserve_attrs = ['SerializeField', 'Header', 'Tooltip', 'Range', 'Min', 'Max']
                if not any(attr in stripped_line for attr in preserve_attrs):
                    i += 1
                    continue
        
        # Skip single-line comments
        if compression_settings.get("remove_comments", False) and stripped_line.startswith('//'):
            # Preserve TODO, FIXME, HACK, NOTE comments as they're often important
            if not any(marker in stripped_line.upper() for marker in ['TODO', 'FIXME', 'HACK', 'NOTE', 'BUG']):
                i += 1
                continue
        
        # Skip regions
        if compression_settings.get("remove_regions", True):
            if stripped_line.startswith('#region') or stripped_line.startswith('#endregion'):
                i += 1
                continue
        
        # Skip common using statements
        if compression_settings.get("remove_using_statements", True):
            common_usings = compression_settings.get("common_usings", [])
            if any(stripped_line == using for using in common_usings):
                i += 1
                continue
        
        # Handle method signature compression
        if compression_settings.get("compress_method_signatures", True) and not collecting_signature:
            method_keywords = ['public', 'private', 'protected', 'internal', 'static', 'virtual', 'override', 'abstract']
            type_keywords = ['void', 'string', 'int', 'float', 'bool', 'double', 'decimal', 'IEnumerator', 'Task', 'async']
            
            if ('(' in stripped_line and ')' not in stripped_line and
                (any(mod in stripped_line for mod in method_keywords) or
                 any(t in stripped_line for t in type_keywords))):
                collecting_signature = True
                signature_parts = [line.rstrip()]
                i += 1
                continue
        
        # Continue collecting signature
        if collecting_signature:
            signature_parts.append(stripped_line)
            if ')' in stripped_line:
                collecting_signature = False
                compressed_signature = ' '.join(part.strip() for part in signature_parts)
                if compression_settings.get("shorten_modifiers", True):
                    compressed_signature = shorten_modifiers(compressed_signature, compression_settings)
                compressed_lines.append(compressed_signature)
                signature_parts = []
            i += 1
            continue
        
        # Compress namespace declarations
        if compression_settings.get("compress_namespaces", True) and stripped_line.startswith('namespace'):
            if i + 1 < len(lines) and lines[i + 1].strip() == '{':
                compressed_lines.append(line.rstrip() + ' {')
                i += 2
                continue
        
        # Skip consecutive empty lines
        if compression_settings.get("remove_empty_lines", True):
            if not stripped_line and compressed_lines and not compressed_lines[-1].strip():
                i += 1
                continue
        
        # Shorten modifiers
        if compression_settings.get("shorten_modifiers", True):
            line = shorten_modifiers(line, compression_settings)
        
        # Trim whitespace
        if compression_settings.get("trim_whitespace", True):
            line = line.rstrip()
        
        # Reduce indentation (big token saver while preserving structure)
        if compression_settings.get("reduce_indentation", True):
            # Count leading whitespace
            stripped = line.lstrip()
            if stripped:  # Don't process empty lines
                leading = line[:len(line) - len(stripped)]
                # Convert tabs to spaces first, then reduce
                leading = leading.replace('\t', '    ')
                # Count indent level (assuming 4-space original)
                indent_level = len(leading) // 4
                remainder = len(leading) % 4
                # Reduce to 1 space per level (configurable)
                new_indent_size = compression_settings.get("indent_size", 1)
                new_leading = ' ' * (indent_level * new_indent_size)
                line = new_leading + stripped
        
        # Compress braces
        if compression_settings.get("compress_braces", True) and stripped_line == '{' and compressed_lines:
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


def shorten_modifiers(line, compression_settings):
    """Shorten access modifiers and keywords.
    
    Uses abbreviations that are:
    - Unambiguous (no conflicts with C# keywords)
    - Recognizable to LLMs
    - Documented in the header legend
    """
    if not compression_settings.get("shorten_modifiers", True):
        return line
    
    # Standard compression - aggressive but unambiguous abbreviations
    # NOTE: 'private' is removed entirely as it's the C# default
    # NOTE: 'internal' is NOT shortened to avoid 'int' type conflict
    replacements = [
        (r'\bprivate\s+', ''),           # private is default, remove entirely
        (r'\bprotected\s+', 'prot '),    # prot = protected
        (r'\bpublic\s+', 'pub '),         # pub = public
        (r'\bstatic\s+', 'stat '),        # stat = static
        (r'\bvirtual\s+', 'virt '),       # virt = virtual
        (r'\babstract\s+', 'abs '),       # abs = abstract  
        (r'\boverride\s+', 'ovr '),       # ovr = override
        (r'\breadonly\s+', 'ro '),        # ro = readonly
        (r'\bconst\s+', 'const '),        # keep const - it's short
        (r'\bsealed\s+', 'seal '),        # seal = sealed
        (r'\basync\s+', 'async '),        # keep async - important
        (r'\bpartial\s+', ''),            # partial can be removed - not critical for understanding
    ]
    
    if compression_settings.get("extreme_compression", False):
        # Extreme mode: even more aggressive
        extreme_replacements = [
            (r'\breturn\s+', 'ret '),         # ret = return (common in asm)
            (r'\bSerializeField\b', 'SF'),    # SF = SerializeField (Unity)
        ]
        replacements.extend(extreme_replacements)
    
    for pattern, replacement in replacements:
        line = re.sub(pattern, replacement, line)
    
    return line


def calculate_compression_stats(original_size, compressed_size):
    """Calculate compression statistics."""
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


# =============================================================================
# FILE COLLECTION
# =============================================================================

def collect_files(project_path, profile):
    """Collect all files matching profile criteria."""
    files = []
    directories = profile.get("directories", [])
    blacklist = profile.get("blacklist_directories", [])
    include_ext = [ext.lower() for ext in profile.get("include_extensions", [])]
    exclude_ext = [ext.lower() for ext in profile.get("exclude_extensions", [])]
    
    for directory in directories:
        scan_path = os.path.join(project_path, directory)
        
        if not os.path.exists(scan_path):
            print(f"  ⚠ Directory not found: {directory}")
            continue
        
        for root, dirs, filenames in os.walk(scan_path):
            # Remove blacklisted directories from traversal
            dirs[:] = [d for d in dirs if d not in blacklist and 
                       not any(bl in os.path.join(root, d) for bl in blacklist)]
            
            for filename in filenames:
                file_ext = os.path.splitext(filename)[1].lower()
                
                # Check inclusion/exclusion
                if include_ext and file_ext not in include_ext:
                    continue
                if file_ext in exclude_ext:
                    continue
                
                full_path = os.path.join(root, filename)
                rel_path = os.path.relpath(full_path, project_path)
                
                # Extract metadata for code files
                metadata = extract_file_metadata(full_path, file_ext)
                
                files.append({
                    'full_path': full_path,
                    'rel_path': rel_path,
                    'filename': filename,
                    'extension': file_ext,
                    **metadata
                })
    
    # Sort by namespace (if available), then by path
    # Use empty string for None namespaces to ensure proper sorting
    return sorted(files, key=lambda x: (x.get('namespace') or '', x['rel_path']))


def extract_file_metadata(file_path, extension):
    """Extract metadata from file (namespace, class name, etc.)."""
    filename_without_ext = os.path.splitext(os.path.basename(file_path))[0]
    
    metadata = {
        'namespace': None,  # None instead of 'Unknown' - we'll handle display later
        'main_class': filename_without_ext
    }
    
    if extension != '.cs':
        return metadata
    
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content_preview = f.read(3000)  # Read more for better detection
            
            # Find namespace (handle file-scoped namespaces too)
            # Standard: namespace Foo.Bar { ... }
            # File-scoped: namespace Foo.Bar;
            ns_match = re.search(r'namespace\s+([\w.]+)\s*[{;]', content_preview)
            if ns_match:
                metadata['namespace'] = ns_match.group(1)
            
            # Find main class/interface/struct/enum
            # Priority: public > internal > private
            class_patterns = [
                r'public\s+(?:abstract\s+)?(?:partial\s+)?(?:sealed\s+)?(?:class|interface|struct|enum)\s+(\w+)',
                r'internal\s+(?:abstract\s+)?(?:partial\s+)?(?:sealed\s+)?(?:class|interface|struct|enum)\s+(\w+)',
                r'(?:abstract\s+)?(?:partial\s+)?(?:sealed\s+)?(?:class|interface|struct|enum)\s+(\w+)',
            ]
            
            for pattern in class_patterns:
                class_match = re.search(pattern, content_preview)
                if class_match:
                    metadata['main_class'] = class_match.group(1)
                    break
                    
    except Exception:
        pass
    
    return metadata


# =============================================================================
# TABLE OF CONTENTS
# =============================================================================

def create_table_of_contents(files, file_locations, profile):
    """Create table of contents."""
    toc = []
    current_namespace = "__INITIAL__"  # Sentinel value
    compact = profile.get("compact_toc", False)
    compression_enabled = profile.get("compression", {}).get("enabled", False)
    
    if not files:
        toc.append("No matching files were found.")
        return toc
    
    for file_info in files:
        # Group by namespace for code files
        if file_info.get('extension') == '.cs' and compression_enabled:
            namespace = file_info.get('namespace')
            if namespace != current_namespace:
                current_namespace = namespace
                if namespace:
                    toc.append(f"\n[{namespace}]")
                else:
                    toc.append(f"\n[Global Scope]")  # Better than "Unknown" or "//"
        
        location = file_locations.get(file_info['rel_path'], {})
        line_info = f"L{location.get('line_num', '?')}"
        
        # Shorten path for display
        short_path = file_info['rel_path']
        for prefix in ["Assets/Scripts/", "Assets/Editor/", "Assets/"]:
            if short_path.startswith(prefix):
                short_path = short_path[len(prefix):]
                break
        
        if compact and file_info.get('extension') == '.cs':
            dir_path = os.path.dirname(short_path)
            if dir_path:
                toc.append(f"  {file_info['main_class']} ({dir_path}/) {line_info}")
            else:
                toc.append(f"  {file_info['main_class']} {line_info}")
        else:
            toc.append(f"- {short_path} (Line: {line_info})")
    
    toc.append("")
    return toc


# =============================================================================
# FILE CLEANUP
# =============================================================================

def clean_previous_files(project_path, profile_name, profile, global_settings, timestamp):
    """Clean up previous output files for a profile."""
    output_filename = profile.get("output_filename", f"EXTRACTED_{profile_name}")
    part_filename = profile.get("part_output_filename", "")
    
    patterns = [os.path.join(project_path, f"{output_filename}*.txt")]
    if part_filename:
        patterns.append(os.path.join(project_path, f"{part_filename}*.txt"))
    
    files_to_clean = []
    for pattern in patterns:
        files_to_clean.extend(glob.glob(pattern))
    
    if not files_to_clean:
        return
    
    # Backup if enabled
    if global_settings.get("backup_previous_files", False):
        backup_dir = os.path.join(project_path, global_settings.get("backup_directory", "_backups"))
        backup_timestamp_dir = os.path.join(backup_dir, timestamp)
        
        os.makedirs(backup_timestamp_dir, exist_ok=True)
        
        for file_path in files_to_clean:
            try:
                filename = os.path.basename(file_path)
                backup_path = os.path.join(backup_timestamp_dir, filename)
                shutil.copy2(file_path, backup_path)
                print(f"  📦 Backed up: {filename}")
            except Exception as e:
                print(f"  ⚠ Failed to backup {filename}: {e}")
    
    # Remove files
    for file_path in files_to_clean:
        try:
            os.remove(file_path)
            print(f"  🗑 Removed: {os.path.basename(file_path)}")
        except Exception as e:
            print(f"  ⚠ Failed to remove {os.path.basename(file_path)}: {e}")


# =============================================================================
# EXTRACTION
# =============================================================================

def extract_profile(project_path, profile_name, profile, global_settings):
    """Extract files for a single profile."""
    print(f"\n{'='*60}")
    print(f"EXTRACTING: {profile_name.upper()}")
    print(f"Description: {profile.get('description', 'No description')}")
    print(f"{'='*60}")
    
    timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    project_name = os.path.basename(project_path)
    
    # Clean previous files
    if global_settings.get("clean_previous_files", True):
        clean_previous_files(project_path, profile_name, profile, global_settings, timestamp)
    
    # Collect files
    print(f"\nScanning directories: {', '.join(profile.get('directories', []))}")
    print(f"Extensions: {', '.join(profile.get('include_extensions', []))}")
    
    files = collect_files(project_path, profile)
    
    if not files:
        print("\n⚠ No files found matching the criteria.")
        print("  Check your settings file to ensure paths are correct.")
        return None
    
    print(f"✓ Found {len(files)} files to extract")
    
    # Initialize content
    all_content = []
    file_locations = {}
    current_line = 1
    
    # Compression settings
    compression_settings = profile.get("compression", {"enabled": False})
    compression_enabled = compression_settings.get("enabled", False)
    
    # Reserve space for header
    header_placeholder_lines = 25 if compression_enabled else 15
    all_content.extend([''] * header_placeholder_lines)
    current_line += header_placeholder_lines
    
    # TOC placeholder
    toc_start_index = len(all_content)
    if profile.get("include_toc", True):
        all_content.append("[TOC_PLACEHOLDER]")
        all_content.append("")
        current_line += 2
    
    # Section separator
    all_content.append("=" * 80)
    all_content.append("FILES")
    all_content.append("=" * 80)
    all_content.append("")
    current_line += 4
    
    # Process files
    total_original_size = 0
    total_compressed_size = 0
    discovered_usings = set()  # Track non-common usings for header
    
    for file_info in files:
        file_path = file_info['full_path']
        rel_path = file_info['rel_path']
        file_ext = file_info.get('extension', '')
        
        # Record location
        file_locations[rel_path] = {'line_num': current_line + 2}
        
        # File header
        all_content.append("/" * 60)
        all_content.append(f"// FILE: {rel_path}")
        all_content.append("")
        current_line += 3
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                original_content = f.read()
            
            # Track non-common usings in C# files
            if file_ext == '.cs' and compression_enabled:
                common_usings = set(compression_settings.get("common_usings", []))
                for line in original_content.split('\n')[:50]:  # Check first 50 lines
                    stripped = line.strip()
                    if stripped.startswith('using ') and stripped.endswith(';'):
                        if stripped not in common_usings:
                            discovered_usings.add(stripped)
            
            # Apply compression based on file type
            if compression_enabled:
                processed_content = compress_content(original_content, compression_settings, file_ext)
            else:
                processed_content = original_content
            
            # Track stats
            total_original_size += len(original_content)
            total_compressed_size += len(processed_content)
            
            # Add content
            content_lines = processed_content.split('\n')
            all_content.extend(content_lines)
            current_line += len(content_lines)
            
        except Exception as e:
            all_content.append(f"// ERROR: Could not read file. {e}")
            current_line += 1
        
        all_content.append("")
        current_line += 1
    
    # Generate and insert TOC
    if profile.get("include_toc", True):
        toc = create_table_of_contents(files, file_locations, profile)
        all_content[toc_start_index] = '\n'.join(toc)
    
    # Generate header
    stats = calculate_compression_stats(total_original_size, total_compressed_size)
    
    header_text = profile.get("header_text", "Extracted files\n")
    try:
        header_text = header_text.format(
            project_name=project_name,
            extraction_date=datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
            original_size=total_original_size,
            compressed_size=total_compressed_size,
            saved_percent=stats['percentage'],
            tokens_saved=stats['tokens_saved']
        )
    except KeyError:
        pass  # Some placeholders may not be in all headers
    
    # Add discovered usings to header if compression is enabled
    if compression_enabled and discovered_usings:
        usings_section = "\n\nProject-specific using statements (add these when needed):\n"
        for using in sorted(discovered_usings):
            usings_section += f"{using}\n"
        header_text += usings_section
    
    # Add modifier legend if shortening is enabled
    if compression_enabled and compression_settings.get("shorten_modifiers", True):
        legend = "\n\nMODIFIER LEGEND:\n"
        legend += "  pub=public | prot=protected | stat=static\n"
        legend += "  virt=virtual | abs=abstract | ovr=override\n"
        legend += "  ro=readonly | seal=sealed\n"
        legend += "  (private & partial are omitted, internal kept as-is)\n"
        if compression_settings.get("extreme_compression", False):
            legend += "  ret=return | SF=SerializeField\n"
        header_text += legend
    
    # Replace header placeholder
    header_lines = header_text.split('\n')
    if len(header_lines) < header_placeholder_lines:
        header_lines.extend([''] * (header_placeholder_lines - len(header_lines)))
    else:
        header_lines = header_lines[:header_placeholder_lines]
    all_content[0:header_placeholder_lines] = header_lines
    
    # Generate filename
    base_filename = profile.get("output_filename", f"EXTRACTED_{profile_name}")
    if global_settings.get("include_timestamp_in_filename", False):
        output_filename = f"{base_filename}_{timestamp}.txt"
    else:
        output_filename = f"{base_filename}.txt"
    
    # Write output
    output_path = os.path.join(project_path, output_filename)
    
    try:
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(all_content))
        
        print(f"\n✓ Success! Output saved to: {output_filename}")
        
        # Show stats
        if global_settings.get("show_compression_stats", True) and compression_enabled:
            print(f"\n📊 Compression Statistics:")
            print(f"   Original:   {total_original_size:>10,} chars ({format_size(total_original_size)})")
            print(f"   Compressed: {total_compressed_size:>10,} chars ({format_size(total_compressed_size)})")
            print(f"   Saved:      {stats['saved']:>10,} chars ({stats['percentage']:.1f}%)")
            print(f"   Est. tokens saved: ~{stats['tokens_saved']:,}")
        
        return {
            'output_file': output_filename,
            'files_processed': len(files),
            'original_size': total_original_size,
            'compressed_size': total_compressed_size,
            'stats': stats
        }
        
    except IOError as e:
        print(f"\n✗ Error: Could not write to output file. {e}")
        return None


def run_extraction(project_path, profile_filter=None):
    """Run extraction for specified profiles."""
    settings = load_settings()
    global_settings = settings.get("global", {})
    profiles = settings.get("profiles", {})
    
    results = {}
    
    print("\n" + "=" * 60)
    print("UNITY ULTIMATE EXTRACTOR")
    print("=" * 60)
    print(f"Project: {project_path}")
    print(f"Time: {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    
    # Determine which profiles to run
    if profile_filter:
        if profile_filter not in profiles:
            print(f"\n✗ Error: Profile '{profile_filter}' not found.")
            print(f"  Available profiles: {', '.join(profiles.keys())}")
            return results
        profiles_to_run = {profile_filter: profiles[profile_filter]}
    else:
        profiles_to_run = {k: v for k, v in profiles.items() if v.get("enabled", False)}
    
    if not profiles_to_run:
        print("\n⚠ No enabled profiles found. Check your settings file.")
        return results
    
    print(f"\nProfiles to run: {', '.join(profiles_to_run.keys())}")
    
    # Run each profile
    for profile_name, profile in profiles_to_run.items():
        result = extract_profile(project_path, profile_name, profile, global_settings)
        if result:
            results[profile_name] = result
    
    # Summary
    if results:
        print("\n" + "=" * 60)
        print("EXTRACTION COMPLETE")
        print("=" * 60)
        
        total_files = sum(r['files_processed'] for r in results.values())
        total_original = sum(r['original_size'] for r in results.values())
        total_compressed = sum(r['compressed_size'] for r in results.values())
        
        print(f"\n📁 Total files processed: {total_files}")
        print(f"📄 Output files created:")
        for profile_name, result in results.items():
            print(f"   - {result['output_file']}")
        
        if total_original > total_compressed:
            overall_stats = calculate_compression_stats(total_original, total_compressed)
            print(f"\n📊 Overall compression: {overall_stats['percentage']:.1f}% reduction")
            print(f"   (~{overall_stats['tokens_saved']:,} tokens saved)")
    
    return results


def list_profiles(settings):
    """List available profiles."""
    profiles = settings.get("profiles", {})
    
    print("\n" + "=" * 60)
    print("AVAILABLE PROFILES")
    print("=" * 60)
    
    for name, profile in profiles.items():
        status = "✓ ENABLED" if profile.get("enabled", False) else "✗ DISABLED"
        description = profile.get("description", "No description")
        directories = profile.get("directories", [])
        extensions = profile.get("include_extensions", [])
        compression = "Yes" if profile.get("compression", {}).get("enabled", False) else "No"
        
        print(f"\n[{name}] {status}")
        print(f"  Description: {description}")
        print(f"  Directories: {', '.join(directories) if directories else 'None'}")
        print(f"  Extensions:  {', '.join(extensions) if extensions else 'All'}")
        print(f"  Compression: {compression}")


# =============================================================================
# MAIN
# =============================================================================

def main():
    parser = argparse.ArgumentParser(
        description="Unity Ultimate Extractor - Extract project files for LLM context",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python unity_extractor.py                    Run all enabled profiles
  python unity_extractor.py --profile scripts  Run only scripts profile
  python unity_extractor.py --profile ui       Run only UI profile
  python unity_extractor.py --list             List available profiles
        """
    )
    
    parser.add_argument(
        '--profile', '-p',
        help='Run specific profile only'
    )
    parser.add_argument(
        '--list', '-l',
        action='store_true',
        help='List available profiles'
    )
    parser.add_argument(
        '--path',
        default=SCRIPT_DIR,
        help='Project path (default: script directory)'
    )
    
    args = parser.parse_args()
    
    if args.list:
        settings = load_settings()
        list_profiles(settings)
    else:
        run_extraction(args.path, args.profile)
    
    # Only wait for key if running without arguments (interactive mode)
    if len(sys.argv) == 1:
        wait_for_any_key()


if __name__ == "__main__":
    main()
