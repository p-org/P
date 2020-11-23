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
