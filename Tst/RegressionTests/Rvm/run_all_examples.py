#!/usr/bin/env python

import glob
import os
import paralleltests
import shutil
import sys
import tempfile
import tools

def usageError():
  raise Exception(
      "Expected exactly one command line argument: "
      "the maximum number of tests to run in parallel.\n"
      "Usage: run_all_examples.py parallel-test-count"
  )

def getTests():
  """ Returns a list of "example" tests.
  """
  script_dir = os.path.dirname(os.path.abspath(__file__))
  unittest_pattern = os.path.join(script_dir, "Example", "*")
  names = [os.path.basename(f) for f in glob.glob(unittest_pattern) if os.path.isdir(f)]
  return sorted([f for f in names if f[0] != '.'])

def buildCommand(test_root, temporary_directory, test_name):
  """ Builds the command to run an "example" test.
  """
  return [ "python"
      , os.path.join(test_root, test_name, "run_test.py")
      , test_name
      , temporary_directory
      ]

def main(argv):
  if len(argv) == 0:
    parallelism = tools.findAvailableCpus() / 2
  elif len(argv) == 1:
    parallelism = int(argv[0])
  else:
    usageError()

  temp_dir = tempfile.mkdtemp()
  tools.progress("Temporary directory: %s" % temp_dir)

  script_dir = os.path.dirname(os.path.abspath(__file__))
  unittest_dir = os.path.join(script_dir, "Example")

  exit_code = paralleltests.runTests(
      parallelism,
      getTests(),
      temp_dir,
      lambda temp_dir, test_name: buildCommand(unittest_dir, temp_dir, test_name))
  shutil.rmtree(temp_dir)
  sys.exit(exit_code)


if __name__ == "__main__":
    main(sys.argv[1:])
