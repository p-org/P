import whatthepatch

def tag_surround(tagname, contents):
    return f"<{tagname}>\n{contents}\n</{tagname}>"

def file_dict_to_prompt(file_dict, pre="", post=""):
    """
    Converts a dictionary of file paths and contents into a single string with XML-style tags.

    Parameters:
    file_dict (dict): Dictionary mapping file paths to file contents
    pre (str): String to prepend to the result
    post (str): String to append to the result

    Returns:
    str: Combined string with each file's content wrapped in XML tags
    """
    result = pre
    
    for filepath, contents in file_dict.items():
        result += f"<{filepath}>\n{contents}\n</{filepath}>\n"
    
    result += post
    return result

def snake_to_title(s):
    """
    Converts a snake_case string to Title Case.
    
    Parameters:
    s (str): Snake case string (e.g., "section_name_here")
    
    Returns:
    str: Title case string (e.g., "Section Name Here")
    """
    return " ".join(word.capitalize() for word in s.split("_"))

def tags_to_md(s, tag_level=4):
    """
    Converts a string with XML-style tagged sections into markdown format.
    
    Parameters:
    s (str): Input string with sections wrapped in tags like <section_name>content</section_name>
    tag_level (int): The heading level to use for section names in markdown (default: 4)
    
    Returns:
    str: Markdown formatted string with section tags converted to headings
    """
    import re
    
    # Initialize result string
    result = ""
    
    # Pattern to match tagged sections: <tag>content</tag>
    pattern = r"<([^>]+)>(.*?)</\1>"
    
    # Find all matches in the input string
    matches = re.finditer(pattern, s, re.DOTALL)
    
    # Process each match
    last_end = 0
    for match in matches:
        # Add any text between matches
        result += s[last_end:match.start()]
        
        # Extract tag name and content
        tag_name = match.group(1)
        content = match.group(2).strip()
        
        # Convert tag name from snake_case to Title Case
        title = snake_to_title(tag_name)
        
        # Create markdown heading with appropriate level
        heading = "#" * tag_level
        
        # Add heading and content to result
        result += f"{heading} {title}\n{content}\n\n"
        
        last_end = match.end()
    
    # Add any remaining text after last match
    result += s[last_end:]
    
    return result.strip()

def add_line_numbers(s):
    """
    Prepends line numbers to each line in the input string.
    
    Parameters:
    s (str): Input string with multiple lines
    
    Returns:
    str: String with line numbers added at the start of each line
    """
    # Split the string into lines while preserving empty lines
    lines = s.splitlines()
    
    # Process each line
    numbered_lines = []
    for i, line in enumerate(lines, 1):
        lstripped = line.lstrip()
        leading_space = len(line) - len(lstripped)

        if not line and not lstripped:
            lstripped = "[empty line]"
        if not lstripped and line:
            lstripped = f"[{leading_space} spaces]"

        # Preserve any leading whitespace
        # Add line number to all lines, including empty ones
        numbered_line = f"{' ' * leading_space}{i}. {lstripped}"
        numbered_lines.append(numbered_line)
    
    # Join the lines back together
    return "\n".join(numbered_lines)

def parse_tags(tag_string):
    """
    Parses a tag string into a dictionary containing the tag name and any attributes.
    
    Parameters:
    tag_string (str): String containing a tag, optionally with attributes
                     e.g. "<PSrc/Example.p start_line=1, end_line=10>"
                     or "<PSrc/Example.p>"
    
    Returns:
    dict: Dictionary containing the tag name and any attributes
          e.g. {"name": "PSrc/Example.p", "start_line": 1, "end_line": 10}
          or {"name": "PSrc/Example.p"}
    """
    # Remove < and > from the string
    tag_content = tag_string.strip("<>")
    
    # Split into name and attributes (if any)
    parts = tag_content.split(" ", 1)
    name = parts[0]
    result = {"name": name}
    
    # If there are attributes, parse them
    if len(parts) > 1:
        attrs = parts[1].strip()
        # Split by comma and handle each key=value pair
        for pair in attrs.split(","):
            pair = pair.strip()
            if "=" in pair:
                key, value = pair.split("=", 1)
                key = key.strip()
                value = value.strip()
                # Try to convert to int if possible
                try:
                    value = int(value)
                except ValueError:
                    pass
                result[key] = value
    
    return result


def apply_patch_whatthepatch_per_file(patch_content_dict, file_contents):
    """
    Apply a unified diff patch using the whatthepatch library.
    
    Requirements:
        pip install whatthepatch
    """
    result = {k:(c, "") for k,c in file_contents.copy().items()}
    
    for fname, patch_content in patch_content_dict.items():
        print(f"Applying patch for {fname}")
        diffs = list(whatthepatch.parse_patch(patch_content))
        print(f"DIFFS LENGTH: {len(diffs)}")
        if len(diffs) > 1:
            raise Exception(f"More than expected diffs per file {len(diffs)}")
        
        diff = diffs[0]
        file_path = diff.header.new_path
        
        if file_path not in file_contents:
            print(f"{file_path} not in file_contents dictionary")
            continue
            
        original_lines = file_contents[file_path].splitlines()

        try:
            # whatthepatch can apply patches directly
            new_content = whatthepatch.apply_diff(diff, original_lines)
            result[file_path] = ('\n'.join(new_content), "")
            
        except Exception as e:
            err_msg = f"Could not apply patch to {file_path}: {e}"
            result[file_path] = (file_contents[file_path], err_msg)
            print(err_msg)
            continue
        
    return result


# Alternative implementation using whatthepatch (another good option)
def apply_patch_whatthepatch(patch_content, file_contents):
    """
    Apply a unified diff patch using the whatthepatch library.
    
    Requirements:
        pip install whatthepatch
    """
    
    # Parse the patch
    diffs = list(whatthepatch.parse_patch(patch_content))
    print(f"DIFFS LENGTH: {len(diffs)}")
    
    # Create a copy of the original file contents
    result = {k:(c, "") for k,c in file_contents.copy().items()}
    
    for diff in diffs:
        file_path = diff.header.new_path
        # print(f"file_path = {file_path}")
        
        if file_path not in file_contents:
            print(f"{file_path} not in file_contents dictionary")
            continue
            
        # Get original content as lines
        original_lines = file_contents[file_path].splitlines()
        # print(f"Original lines = {len(original_lines)}")
        # print(f"Diff = {diff}")
        # Apply the changes
        try:
            # whatthepatch can apply patches directly
            new_content = whatthepatch.apply_diff(diff, original_lines)
            result[file_path] = ('\n'.join(new_content), "")
            
        except Exception as e:
            err_msg = f"Could not apply patch to {file_path}: {e}"
            result[file_path] = (file_contents[file_path], err_msg)
            print(err_msg)
            continue
    
    return result

def parse_patches_by_file(patch_content):
    """
    Parse a string containing multiple patches and return a dictionary
    mapping file paths to their individual patch content.
    
    Args:
        patch_content (str): String containing multiple patches
        
    Returns:
        dict: Dictionary mapping file paths to patch strings
    """
    patches_by_file = {}
    
    lines = patch_content.strip().split('\n')
    current_patch_lines = []
    current_file = None
    
    i = 0
    while i < len(lines):
        line = lines[i]
        
        # Check if this is the start of a new patch
        if line.startswith('--- '):
            # Save previous patch if it exists
            if current_file and current_patch_lines:
                patches_by_file[current_file] = '\n'.join(current_patch_lines)
            
            # Start new patch
            current_patch_lines = [line]
            
            # Get the next line which should be the +++ line
            if i + 1 < len(lines) and lines[i + 1].startswith('+++ '):
                i += 1
                plus_line = lines[i]
                current_patch_lines.append(plus_line)
                
                # Extract file path from +++ line
                current_file = plus_line[4:].split('\t')[0].strip()
            
        else:
            # Add line to current patch
            if current_patch_lines:
                current_patch_lines.append(line)
        
        i += 1
    
    # Don't forget the last patch
    if current_file and current_patch_lines:
        patches_by_file[current_file] = '\n'.join(current_patch_lines)
    
    return patches_by_file


def parse_patches_by_file_markdown(patch_content):
    """
    Parse a string containing multiple patches and return a dictionary
    mapping file paths to their individual patch content.
    
    Args:
        patch_content (str): String containing multiple patches
        
    Returns:
        dict: Dictionary mapping file paths to patch strings
    """
    patches_by_file = {}
    
    lines = patch_content.strip().split('\n')
    current_patch_lines = []
    current_file = None
    
    i = 0
    while i < len(lines):
        line = lines[i]
        
        # Check if this is the start of a new patch
        if line.startswith('--- '):
            # Save previous patch if it exists
            if current_file and current_patch_lines:
                patches_by_file[current_file] = '\n'.join(current_patch_lines)
            
            # Start new patch
            current_patch_lines = [line]
            
            # Get the next line which should be the +++ line
            if i + 1 < len(lines) and lines[i + 1].startswith('+++ '):
                i += 1
                plus_line = lines[i]
                current_patch_lines.append(plus_line)
                
                # Extract file path from +++ line
                current_file = plus_line[4:].split('\t')[0].strip()
            
        else:
            # Add line to current patch
            if current_patch_lines:
                current_patch_lines.append(line)
        
        i += 1
    
    # Don't forget the last patch
    if current_file and current_patch_lines:
        patches_by_file[current_file] = '\n'.join(current_patch_lines)
    
    return patches_by_file


def parse_patches_by_file_alternative(patch_content):
    """
    Alternative implementation using regex to split patches.
    More robust for complex cases.
    """
    import re
    
    patches_by_file = {}
    
    # Split on lines that start with "--- " (but not in the middle of content)
    # This regex looks for "--- " at the start of a line
    patch_sections = re.split(r'\n(?=--- )', patch_content.strip())
    
    for section in patch_sections:
        if not section.strip():
            continue
            
        lines = section.split('\n')
        
        # Find the +++ line to get the file path
        file_path = None
        for line in lines:
            if line.startswith('+++ '):
                file_path = line[4:].split('\t')[0].strip()
                break
        
        if file_path:
            patches_by_file[file_path] = section
    
    return patches_by_file