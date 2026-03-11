import re
from utils import file_utils


def get_all_P_files(text):
    """Extract P file paths from compiler output and map filenames to full paths."""
    p_file_pattern = r'file: ([\S\s]+?\.p)'
    return file_utils.map_p_file_to_path(re.findall(p_file_pattern, text))
