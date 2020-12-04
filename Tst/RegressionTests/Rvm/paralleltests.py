import os
import subprocess
import threading
import time
import tools

class Test:
  """
  Test runner based on shell commands.

  The constructor starts the test. You should call close() when finished.
  """
  def __init__(self, name, temporary_directory, command_creator):
    self.__name = name

    temporary_directory = os.path.join(temporary_directory, name)
    if not os.path.exists(temporary_directory):
      os.makedirs(temporary_directory)

    self.__process = subprocess.Popen(
        command_creator(temporary_directory, name),
        stdout = subprocess.PIPE,
        stderr = subprocess.PIPE,
        universal_newlines = True)

    self.__stdout = []
    self.__stdout_thread = threading.Thread(
        target=Test.__readStream,
        args=(self.__process.stdout, self.__stdout))
    self.__stdout_thread.daemon=True
    self.__stdout_thread.start()

    self.__stderr = []
    self.__stderr_thread = threading.Thread(
        target=Test.__readStream,
        args=(self.__process.stderr, self.__stderr))
    self.__stderr_thread.daemon=True
    self.__stderr_thread.start()

  @staticmethod
  def __readStream(stream, output):
    for line in stream:
      output.append(line)

  def __returnCode(self):
    self.__process.poll()
    return self.__process.returncode

  def failed(self):
    retv = self.__returnCode()
    return (retv is not None) and (retv != 0)

  def isRunning(self):
    return self.__returnCode() is None

  def close(self):
    self.__stdout_thread.join(1)  # 1 sec
    self.__stderr_thread.join(1)  # 1 sec
    pass

  def closeAndPrintFailure(self):
    assert not self.isRunning()
    self.close()
    tools.progress("Test %s failed!" % self.__name)
    print('')
    tools.progress("Stdout:")
    print(''.join(self.__stdout))
    print('')
    tools.progress("Stderr:")
    print(''.join(self.__stderr))
    print('')
    print('')

  def name(self):
    return self.__name

def runTests(parallelism, test_names, temporary_directory, command_creator):
  """Runs a test suite.

  Args:
    parallelism (int): maximum number of tests to run in parallel.
    test_names (list of string): the names of all tests in the suite.
    temporary_directory (string): path of a directory for temporary files.
    command_creator (func(string, string) : list of string): function that
        receives a temporary directory and a test name and produces the
        command which runs that test.

  Returns:
    0 for success, 1 for failure.
  """
  running = []
  failed = []
  test_count = len(test_names)
  while running or test_names:
    time.sleep(0.1)  # 0.1 sec

    new_running = []
    for test in running:
      if test.isRunning():
        new_running.append(test)
      elif test.failed():
        test.closeAndPrintFailure()
        failed.append(test)
      else:
        print("%s finished" % test.name())
        test.close()

    running = new_running

    while test_names and len(running) < parallelism:
      print("Starting: %s" % test_names[0])
      running.append(Test(test_names[0], temporary_directory, command_creator))
      test_names = test_names[1:]

  if failed:
    tools.progress(
        "Finished running, %d out of %d tests failed."
        % (len(failed), test_count))
    return 1
  else:
    tools.progress("Finished running, all tests passed.")
    return 0
