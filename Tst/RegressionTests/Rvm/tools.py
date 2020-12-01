import os.path
import subprocess

class SubprocessError(RuntimeError):
    def __init__(self, command):
        super(SubprocessError, self).__init__("Error while running %s." % command)

def runNoError(command):
    retv = subprocess.call(command)
    if retv != 0:
        raise SubprocessError(command)

def runInDirectory(directory, func):
    current_dir = os.getcwd()
    os.chdir(directory)
    try:
        func()
    finally:
        os.chdir(current_dir)

def progress(message):
    print("====== %s" % message)

def readFile(name):
    """
    Reads a text file.

    Args:
        name (str): The name of the file

    Returns:
        str: The content of the file.
    """
    with open(name, "rt") as f:
        return ''.join(f)

def writeFile(name, content):
    """
    Writes a text file.

    Args:
        name (str): The name of the file
        content (str): The content of the file.
    """
    with open(name, "wt") as f:
        f.write(content)
