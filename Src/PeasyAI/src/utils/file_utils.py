import os, logging, json, shutil, re
from utils import global_state as globals

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO)

def read_file(file_path):
    """
    Reads the content of a file in the local directory.

    Parameters:
    file_path (str): The path of the file to be read.

    Returns:
    str: The content of the file.
    """
    try:
        if not os.path.isabs(file_path):
            file_path = os.path.join(os.getcwd(), file_path)
        with open(file_path, 'r') as file:
            content = file.read()
        return content
    except Exception as e:
        raise e
    

def read_file_line(file_path, line_number):
    """
    Reads a single line of a file in the local directory.

    Parameters:
    file_path (str): The path of the file to be read.
    line_number (int): The line number of the file we want to read

    Returns:
    str: The content of the single line from the file.
    """
    try:
        file_path = os.path.join(os.getcwd(), file_path)
        with open(file_path, 'r') as file:
            lines = file.readlines()
            if line_number <= len(lines):
                specific_line = lines[line_number - 1]  # Convert to 0-based index
                return specific_line  # Use strip() to remove any leading/trailing whitespace
            else:
                raise ValueError(f"Error: The file only has {len(lines)} lines.")
    except FileNotFoundError:
        return f"Error: The file '{file_path}' was not found."
    except IOError as e:
        return f"Error: An IOError occurred while reading the file '{file_path}'. Details: {e}"
    except Exception as e:
        return e
    

def write_file(file_path, content):
    """
    Writes content to a file, creating the directory and file if they do not exist.

    Parameters:
    file_path (str): The path to the file to be written to.
    content (str): The content to be written to the file.

    Returns:
    bool: true if the write operation was successful, false otherwise
    """
    try:
        file_path = os.path.join(os.getcwd(), file_path)
        dir_name = os.path.dirname(file_path)
        ensure_dir_exists(dir_name)
        
        with open(file_path, 'w') as file:
            file.write(content)
        return True
    except IOError as e:
        logger.info(f"Error: An IOError occurred while writing to the file '{file_path}'. Details: {e}")
        return False


def empty_file(file_path):
    """
    Empties the content of a file.

    Parameters:
    file_path (str): The path to the file to be emptied.

    Returns:
    bool: true if the empty operation was successful, false otherwise
    """
    try:
        file_path = os.path.join(os.getcwd(), file_path)
        with open(file_path, 'w') as file:
            file.truncate(0)
        return True
    except IOError as e:
        logger.info(f"Error: An IOError occurred while emptying the file '{file_path}'. Details: {e}")
        return False


def append_file(file_path, content):
    """
    Appends content to a file.
    
    Parameters:
    file_path (str): The path to the file to be written to.
    content (str): The content to be appended to the file.

    Returns:
    bool: true if the append operation was successful, false otherwise.
    """
    try:
        file_path = os.path.join(os.getcwd(), file_path)
        with open(file_path, 'a') as file:
            file.write(content)
        return True
    except IOError as e:
        logger.info(f"Error: An IOError occurred while appends to the file '{file_path}'. Details: {e}")
        return False


def read_json_file(file_path):
    """
    Reads the content of a JSON file.

    Parameters:
    file_path (str): The path of the JSON file to be read.

    Returns:
    data (dict): The content of the JSON file as a dictionary.
    """
    try:
        file_path = os.path.join(os.getcwd(), file_path)
        with open(file_path, 'r') as file:
            data = json.load(file)
        return data
    except FileNotFoundError:
        return f"Error: The file '{file_path}' was not found."
    except json.JSONDecodeError:
        return f"Error: The file '{file_path}' is not a valid JSON file."
    except IOError as e:
        return f"Error: An IOError occurred while reading the file '{file_path}'. Details: {e}"


def ensure_dir_exists(dir_path):
    """
    Ensure that a directory exists; create it if it doesn't.

    Parameters:
    dir_path (str): The relative or absolute path of the directory to check or create.

    Creates:
    If the directory does not exist, it will be created along with any necessary parent directories.
    """
    dir_path = os.path.join(os.getcwd(), dir_path)
    if not os.path.exists(dir_path):
        os.makedirs(dir_path)


def is_dir_empty(dir_path):
    """
    Check if a directory is empty.

    Parameters:
    dir_path (str): The relative or absolute path of the directory to check.

    Returns:
    bool: True if the directory is empty, False otherwise.
    """
    dir_path = os.path.join(os.getcwd(), dir_path)
    return not os.listdir(dir_path)


def move_dir_contents(source_dir, destination_dir):
    """
    Move all contents from the source directory to the destination directory.

    Parameters:
    source_dir (str): The relative or absolute path of the source directory.
    destination_dir (str): The relative or absolute path of the destination directory.

    Moves:
    All files and subdirectories from the source directory to the destination directory.
    """
    source_dir = os.path.join(os.getcwd(), source_dir)
    destination_dir = os.path.join(os.getcwd(), destination_dir)

    for item in os.listdir(source_dir):
        source_path = os.path.join(source_dir, item)
        destination_path = os.path.join(destination_dir, item)
        logger.info("Moving %s to %s", source_path, destination_path)
        shutil.move(source_path, destination_path)


def check_directory(file_path):
    """
    Returns whether the file path is to a directory or not.
    Parameters:
    file_path (str): The path to the file to check

    Returns: bool: Returns True if the file path is a directory and False if the file path is not a directory
    """
    return os.path.isdir(os.path.join(os.getcwd(), file_path))

def create_directory(path):
    os.mkdir(os.path.join(os.getcwd(), path))
    

def list_top_level_contents(directory):
    """
    Lists the top-level contents of the specified directory.
    Parameters:
    directory (str): The path to the directory to list contents of.
    
    Returns: A list of top-level contents in the directory.
    """
    try:
        directory = os.path.join(os.getcwd(), directory)
        contents = os.listdir(directory)
        return contents
    except Exception as e:
        return str(e)


def get_recent_project_path():
    """
    Returns the path to the most recent P project.

    Returns:
    file_path (str): The path to the most recent project, or None
    """
    if globals.custom_dir_path != None:
        file_path = os.path.join(globals.custom_dir_path, globals.project_name_with_timestamp)
        return file_path
    if check_directory(globals.recent_dir_path):
        for proj in list_top_level_contents(globals.recent_dir_path):
            file_path = os.path.join(os.getcwd(), globals.recent_dir_path + "/" + proj)
            return file_path
    return None

def map_p_file_to_path(p_files):
    """
    Maps P file names to their full paths.

    Parameters:
    p_files (list): List of P file paths.

    Returns: mapped_files (dict): A dictionary where the keys are P file names and the values are their full paths.
    """
    mapped_files = {}
    for file in p_files:
        mapped_files[os.path.basename(file)] = file
    return mapped_files


def generate_file_name_for_dup(file_list, file_name):
    """
    Generates a new file name if there are duplicates in the logs directory
    Parameters:
    file_list (list): The path to the directory to list contents of.
    file_name (str): The name of the project that may be a a duplicate of another project.
    
    Returns: 
    new_name (str): The same file_name or a different one with a number attached to it
    """
    new_name = file_name
    num_iterations = 1
    while new_name in file_list:
        new_name = file_name + " (" + str(num_iterations) + ")"
        num_iterations = num_iterations + 1
    return new_name


def extract_timestamp(directory_path):
    """
    Extract the timestamp (creation time) of the directory.
    Parameters: 
    directory_path (str): The path of a directory 

    Returns: The time the directory was created. 
    """
    return os.stat(directory_path).st_ctime



def get_modules_file(p_files):
    """
    Finds the first P file in the provided dictionary that ends with 'Modules.p'.

    Parameters:
    p_files (dict): Dictionary of P file paths.

    Returns:
    module_file_name (str): The file path that ends with 'Modules.p', or None if no such file is found.
    """
    try:
        module_file_name = None
        for f in p_files:
            if f.endswith("Modules.p"):
                module_file_name = f
        if not module_file_name:
            for f in p_files:
                curr_path = p_files[f]
                if "PSrc" in curr_path:
                    file_contents = read_file(curr_path)
                    if "module" in file_contents:
                        module_file_name = f
        return module_file_name
    except:
        raise ValueError("An error occured: No modules were created in the P project. Please try again.")

def get_last_file(error_msg):
    """
    Extracts the last file name from the error message.

    Parameters:
    error_msg (str): The error message to parse.

    Returns: str: The last file name found in the error message.
    """
    dup_file_regex = r'[\s\S]+ [\s\S]+\/([\s\S]+.p)'
    match = re.search(dup_file_regex, error_msg)
    logger.info(match.group())
    return match.group(1)


def get_all_files(file_path, filter_ext=None):
    """
    Retrieves all files in the given directory or returns the file if a single file is specified.
    Can optionally filter files by extension.

    Parameters:
    file_path (str): The path to the file or directory to be scanned.
    filter_ext (list, optional): List of file extensions to include (without the dot). 
                               For example: ["p", "pproj"]

    Returns:
    file_path (list): A list of file paths found in the directory or a single file path if a file is provided.

    Raises:
    ValueError: If the file path does not exist.
    """
    all_files = []
    if not os.path.exists(file_path):
        raise ValueError("The file path provided does not exist. ")
    
    if check_directory(file_path):
        for root, dirs, files in os.walk(file_path):
            for file in files:
                # If filter_ext is provided, only include files with matching extensions
                if filter_ext is not None:
                    ext = os.path.splitext(file)[1].lstrip('.')
                    if ext not in filter_ext:
                        continue
                all_files.append(os.path.join(root, file))
    elif os.path.isfile(file_path):
        # For single file, check extension if filter is provided
        if filter_ext is not None:
            ext = os.path.splitext(file_path)[1].lstrip('.')
            if ext in filter_ext:
                all_files.append(file_path)
        else:
            all_files.append(file_path)
    
    return all_files

def combine_files(doc_list, pre="", post="", preprocessing_function=lambda _, c:c):
    combined_text = ""
    for doc_path in doc_list:
        # Read the actual document content
        content = read_file(doc_path)
        
        # Apply preprocessing to each document and append to combined text
        processed_content = preprocessing_function(doc_path, content)
        combined_text += processed_content
    
    combined_text = f"{pre}{combined_text}{post}"
    return combined_text

def capture_project_state(project_dir):
    """
    Captures the state of all files in a project directory.

    Parameters:
    project_dir (str): Path to the project directory

    Returns:
    dict: Dictionary mapping relative file paths to file contents
    """
    state = {}
    project_dir = os.path.abspath(project_dir)
    
    # Get all files in the project directory
    files = get_all_files(project_dir, filter_ext=["p", "pproj", "ddoc"])
    
    # Read each file and store with relative path
    for file_path in files:
        rel_path = os.path.relpath(file_path, project_dir)
        state[rel_path] = read_file(file_path)
    
    return state

def copy_file(src, dest):
    """
    Copies a single file from source to destination.

    Parameters:
    src (str): Source file path
    dest (str): Destination file path

    Raises:
    ValueError: If source is a directory
    FileNotFoundError: If source doesn't exist
    """
    src = os.path.join(os.getcwd(), src)
    dest = os.path.join(os.getcwd(), dest)
    
    if os.path.isdir(src):
        raise ValueError("Source is a directory. Use copytree for directories.")
    
    # Create parent directories if needed
    dest_dir = os.path.dirname(dest)
    ensure_dir_exists(dest_dir)
    
    shutil.copy2(src, dest)

def write_project_state(state, write_dir):
    """
    Writes files from a project state dictionary to a directory.

    Parameters:
    state (dict): Dictionary mapping relative file paths to file contents
    write_dir (str): Directory to write the files to
    """
    # Create the write directory if it doesn't exist
    ensure_dir_exists(write_dir)
    
    # Write each file
    for rel_path, content in state.items():
        full_path = os.path.join(write_dir, rel_path)
        write_file(full_path, content)

def make_copy(src_dir, dest_dir, new_name=None):
    os.makedirs(dest_dir, exist_ok=True)
    dir_name = new_name if new_name else os.path.basename(src_dir)
    new_dir = os.path.join(dest_dir, dir_name)
    shutil.copytree(src_dir, new_dir, dirs_exist_ok=True)
    return new_dir

def write_output_streams(result, captured_streams_output_dir=None):
    if not captured_streams_output_dir:
        return
    
    stdout_file = f"{captured_streams_output_dir}/stdout.txt"
    stderr_file = f"{captured_streams_output_dir}/stderr.txt"

    if not result:
        with open(stdout_file, 'w') as f:
            f.write("..... Found 1 bug.\nTest timed out.\nThis message was written manually")
        return 
    
    if stdout_file:
        with open(stdout_file, 'wb') as f:
            f.write(result.stdout)
    
    if stderr_file:
        with open(stderr_file, 'wb') as f:
            f.write(result.stderr)
