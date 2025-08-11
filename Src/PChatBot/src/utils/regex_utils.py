import re, logging
from utils import file_utils

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")

def replace_not_in(code):
    """
    Replaces occurrences of the invalid '!in' operator with the correct '!(... in ...)' syntax in a given string.
    """
    # Pattern to match '!in' operator
    pattern = r'(\w+(?:\.\w+)*)\s*!in\s*([\w().]+(?:\.\w+)*)'
    # Replace all occurrences
    return re.sub(pattern, lambda m: f'!({m.group(1)} in {m.group(2)})', code)


def add_comma_to_single_field_tuple(code):
    """
    Adds a comma to single-field tuples in a given string.
    """
    # Pattern to match single-field tuples
    pattern = re.compile(r'\(([^,()=><!]+=[^,()=><!]+)\)')

    # Function to add a comma inside the tuple
    def add_comma(match):
        return f"({match.group(1)}, )"
    
    # Replace all occurrences
    return pattern.sub(add_comma, code)


def remove_this_dot(code):
    """
    Removes 'this.' from the input string if present, preserving preceding characters.
    """
    # Pattern to match 'this.' (case-insensitive) while capturing preceding characters
    pattern = r'(^|.)(this\.)'
    # Replace all occurrences
    return re.sub(pattern, lambda m: m.group(1), code, flags=re.IGNORECASE)


def replace_on_ignore(code):
    """
    Replace 'on <event> ignore;' with 'ignore <event>;' in the given text.
    """
    pattern = r'on\s+(\w+)\s+ignore;'
    replacement = r'ignore \1;'
    # Replace all occurrences
    return re.sub(pattern, replacement, code)


def extract_name_from_error(error_message):
    """
    Extract the name from the error message using regex.
    """
    match = re.search(r"'(.+)'", error_message)
    if match:
        return match.group(1)
    return ""


def get_all_P_files(text):
    p_file_pattern = r'file: ([\S\s]+?\.p)'
    return file_utils.map_p_file_to_path(re.findall(p_file_pattern, text))


def parse_compiler_error(error):
    """
    Extracts key information from an error message.

    Returns:
    tuple: (filename, line_number, column_number, error_description) if parsed successfully,
           None otherwise.

    Example:
    >>> error_msg = '''[Parser Error:]
    ... [comp.p] parse error: line 211:14 mismatched input '!' expecting ')'
    ... ~~ [PTool]: Thanks for using P! ~~'''
    >>> parse_error_message(error_msg)
    ('comp.p', '211', '14', " mismatched input '!' expecting ')'")
    """
    patterns = {
        'general': r'Error:\]\s+\[(\S+)\][\s\S]+line (\d+):(\d+)([\s\S]*?)~~',
        'duplicate_declaration': r'Error:\]\s+\[\S+\/(\S+)\:(\d+):(\d+)]([\s\S]+?)~~'
    }

    for key, pattern in patterns.items():
        match = re.search(pattern, error)
        if match:
            logger.info(f"{key.upper()} ERROR: {match.group()}")
            return match.groups()
    
    logger.info("No matching error pattern found")
    return None


def parse_testdrivers(file_contents):
    """
    Parses the contents of a test driver file to extract machine names.

    Parameters:
    file_contents (str): The contents of the Test Driver file as a string.

    Returns:
    list: A list of machine names (str) extracted from the file contents.
    """
    machine_pattern = r"machine\s+(\S+)"
    machines = re.findall(machine_pattern, file_contents)
    return machines


def extract_Pcode(llm_response):
    """
    Extracts P code from the LLM response and returns the result.
 
    Parameters:
    llm_response (str): The response from the LLM containing P code.
 
    Returns:
    Pcode (str): The extracted P code.
    """
    p_file_pattern = r'<(\w+)>(.*?)</\1>'
 
    # Extract file content
    Pcode = llm_response
    match = re.search(p_file_pattern, llm_response, re.DOTALL)
    if match:
        Pcode = match.group(2).strip()
    return Pcode

def general_replace(pattern, replacement, p_code, correction_string):
    """
    Replaces occurrences of a pattern in the P code with a specified replacement,
    logs the changes, and returns the modified P code.
    
    Parameters:
    pattern (str): The regex pattern to search for in the P code.
    replacement (str): The string to replace the matched pattern with.
    p_code (str): The original P code to be modified.
    correction_string (str): The message to print when a replacement occurs.
    
    Returns: 
    p_code (str): The modified P code after performing the replacements.
    """
    new_final_P_code = re.sub(pattern, replacement, p_code)
    if new_final_P_code != p_code:
        p_code = new_final_P_code
    logger.info(correction_string)
    return p_code


def get_enum_def(type_name, code):
    """
    Searches for the definition of an enum with a specified type name in the provided code
    and returns the matching enum block as a list.

    Parameters:
    type_name (str): The name of the enum type to search for.
    code (str): The code to search within.

    Returns:
    list: A list containing the enum block if found, otherwise an empty list.
    """
    pattern = "enum\s+" + type_name + "\s*{\s*[^}]*\s*}"

    # Search for the type_name enum block
    match = re.search(pattern, code)

    if match:
        enum_block = match.group(0)
        return [enum_block]
    return []