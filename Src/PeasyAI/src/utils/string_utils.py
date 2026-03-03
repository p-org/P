import whatthepatch
import re

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


def extract_tag_contents(full_string, tag_name):
    """
    Extracts the contents between XML-style tags in a multiline string.
    
    Parameters:
    full_string (str): The input string containing tagged content
    tag_name (str): The name of the tag to extract content from
    
    Returns:
    str: The content between the opening and closing tags, or None if not found
    """
    pattern = f"<{tag_name}>(.*?)</{tag_name}>"
    match = re.search(pattern, full_string, re.DOTALL)
    if match:
        return match.group(1).strip()
    return None


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