"""
Utility functions for creating P project structure.
"""

import os
import shutil
from utils import file_utils, global_state

def create_project_directories(project_root):
    """
    Creates the standard P project directories.
    
    Args:
        project_root: Root directory for the P project
    """
    directories = [
        os.path.join(project_root, "PSrc"),
        os.path.join(project_root, "PSpec"),
        os.path.join(project_root, "PTst"),
        os.path.join(project_root, "PGenerated"),
    ]
    
    for directory in directories:
        os.makedirs(directory, exist_ok=True)
    
    return directories

def generate_pproj_file(project_root, project_name):
    """
    Creates a .pproj file in the project root directory.
    
    Args:
        project_root: Root directory for the P project
        project_name: Name of the P project
    """
    pproj_template = file_utils.read_file(global_state.pproj_template_path)
    pproj_content = pproj_template.replace("{project_name}", project_name)
    
    pproj_path = os.path.join(project_root, f"{project_name}.pproj")
    file_utils.write_file(pproj_path, pproj_content)
    print("Crerated Project Structure :white_check_mark:")
    
    return pproj_path

def setup_project_structure(project_root, project_name):
    """
    Sets up the complete P project structure including directories and .pproj file.
    
    Args:
        project_root: Root directory for the P project
        project_name: Name of the P project
        
    Returns:
        Dictionary with created directories and files
    """
    # Create project directories
    directories = create_project_directories(project_root)
    
    # Create .pproj file
    pproj_path = generate_pproj_file(project_root, project_name)
    
    return {
        "directories": directories,
        "pproj_file": pproj_path
    }
