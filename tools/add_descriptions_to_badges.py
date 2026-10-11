#!/usr/bin/env python3
"""
Add descriptions from commands_by_entity_clean.json to monster_badges.json.
"""

import json
import os

def main():
    # File paths
    script_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.dirname(script_dir)

    commands_file = os.path.join(project_root, "docs/pdf_extracted/commands_by_entity_clean.json")
    badges_file = os.path.join(project_root, "Dom5Editor.Core/Data/monster_badges.json")

    # Load commands reference
    with open(commands_file, 'r', encoding='utf-8') as f:
        commands_data = json.load(f)

    # Build dict of command names to descriptions (strip # prefix)
    command_descriptions = {}
    for cmd in commands_data.get('monster', []):
        name = cmd.get('name', '')
        if name.startswith('#'):
            name = name[1:]  # Strip # prefix
        description = cmd.get('description', '').strip()
        if name and description:
            command_descriptions[name] = description

    print(f"Loaded {len(command_descriptions)} command descriptions")

    # Load monster_badges.json
    with open(badges_file, 'r', encoding='utf-8') as f:
        badges_data = json.load(f)

    # Track stats
    added_count = 0
    missing_count = 0
    missing_commands = []

    # Process all sections
    for section in badges_data.get('sections', []):
        for cmd in section.get('commands', []):
            cmd_name = cmd.get('name', '')
            if cmd_name in command_descriptions:
                cmd['description'] = command_descriptions[cmd_name]
                added_count += 1
            else:
                missing_count += 1
                missing_commands.append(cmd_name)

    print(f"Added {added_count} descriptions")
    print(f"Missing {missing_count} descriptions: {missing_commands}")

    # Write updated badges file
    with open(badges_file, 'w', encoding='utf-8') as f:
        json.dump(badges_data, f, indent=2)

    print(f"Updated {badges_file}")

    # Output first few commands to show result
    print("\nFirst few commands with descriptions:")
    for section in badges_data.get('sections', [])[:2]:
        print(f"\n=== {section.get('displayName')} ===")
        for cmd in section.get('commands', [])[:5]:
            print(f"  {cmd.get('name')}: {cmd.get('description', '(no description)')[:60]}...")

if __name__ == "__main__":
    main()
