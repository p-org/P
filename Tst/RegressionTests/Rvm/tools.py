import sys
import os.path
import subprocess

class SubprocessError(RuntimeError):
    def __init__(self, command):
        super(SubprocessError, self).__init__("Error while running %s." % command)

def runNoError(command):
    """
    Runs a shell command, raising an exception for failures.

    Args:
        command (list of str): The command to run, in the same format as
            for subprocess.call.

    Raises:
        SubprocessError if the command returned an error code.
    """
    retv = subprocess.call(command)
    if retv != 0:
        raise SubprocessError(command)

def runInDirectory(directory, func):
    """
    Runs a function using the given directory as the current one.

    runInDirectory changes the current directory, runs the function,
    then reverts to the old current directory.

    Args:
        directory (str): The current directory for the duration
            of the function.
        func (function): The function to run.
    """
    current_dir = os.getcwd()
    os.chdir(directory)
    try:
        func()
    finally:
        os.chdir(current_dir)

def progress(message):
    """
    Prints a progress message.

    Args:
        message (str): The message to print
    """
    print("====== %s" % message)
    sys.stdout.flush()

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

def findAvailableCpus():
    """
    Attempts to estimate the number of CPUs available for the current process.
    """
    try:
        return len(os.sched_getaffinity(0))
    except:
        pass
    try:
        import multiprocessing
        # This will actually return the number of CPUs available on the system,
        # not the number of CPUs available to the current process.
        return multiprocessing.cpu_count()
    except:
        pass
    return 1
