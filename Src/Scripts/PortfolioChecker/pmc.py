#!/usr/bin/env python3

##############################
# P Model Checker in Portfolio
##############################

import argparse
import json
import multiprocessing
import os
import shutil
import subprocess
import sys
import time
import logging

version = 0.4
start_time = time.time()
DEFAULT_MAX_NUM_WORKERS = (multiprocessing.cpu_count() - 1)
PC = "pc"
if os.environ.get('PC') is not None:
    PC = os.getenv('PC')

configArgs = None
projectName = ""
projectPath = ""
outputPath = ""
dllFile = ""
psymJarFile = ""
maxNumWorkers = DEFAULT_MAX_NUM_WORKERS
commands = []
commandsRun = []
processes = {}
allWorkers = []
pendingWorkers = []
runningWorkers = set()
completedWorkers = set()
buggyWorkers = set()
provedWorkers = set()
startTime = time.time()
outFile = ""
logger = None
psymModes = [ "random", "dfs", "learn", "bmc" ]

header = """-----------------------
PMC -- Portfolio Runner
-----------------------"""

class Worker(object):
    def __init__(self, method, category):
        global outputPath
        self.id = len(allWorkers)
        self.method = method
        self.category = category
        self.strategy = ""
        self.status = None
        self.cmd = None
        self.path = ""

    def __str__(self):
        return f"worker{self.id}"

    def __repr__(self):
        return self.__str__()

    def set_path(self, strategy):
        self.strategy = strategy
        self.path = f"{outputPath}/{str(self)}_{self.category}_{self.strategy}_{method_pretty_name(self.method)}"

    def get_id(self):
        return self.id

    def get_category(self):
        return self.category

    def get_path(self):
        return self.path

    def get_status(self):
        return self.status

    def set_status(self, status):
        self.status = status

    def get_cmd(self):
        return self.cmd

    def set_cmd(self, cmd):
        self.cmd = cmd

class Logger:
    def __init__(self, filename):
        self.file = open(filename, 'w')
        self.stdout = sys.stdout

    def __del__(self):
        self.file.close()
        sys.stdout = self.stdout

    def write(self, message):
        self.file.write(message)
        self.stdout.write(message)

    def flush(self):
        self.file.flush()
        self.stdout.flush()


def get_cli_args(opt_header):
    p = argparse.ArgumentParser(description=str(opt_header), formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('-p', '--project-path', help='path to P project', type=str, required=True)
    p.add_argument('-nproc', help='max number of parallel workers (default: %s)' % DEFAULT_MAX_NUM_WORKERS, type=int,
                   default=DEFAULT_MAX_NUM_WORKERS)
    args, leftovers = p.parse_known_args()
    return args, p.parse_args()


def get_config_args(config_location):
    """
    Parses the CI config file that come with each project in the repository.
    e.g., tests/Tutorial/1_ClientServer/portfolio-config.json
    """
    parser = argparse.ArgumentParser()

    with open(config_location) as handle:
        config_data = json.load(handle)

    for flag_name, flag_info in config_data.items():
        if "default" not in flag_info:
            logger.error("Malformed configuration file: the flag should have a default value")
            sys.exit(1)

        if type(flag_info["default"]) in [int, float]:
            flag_info["type"] = type(flag_info["default"])
        elif isinstance(flag_info["default"], list):
            flag_info["nargs"] = "+"

        flag_info["help"] = f"{flag_info['help']}. Default: '%(default)s'."
        parser.add_argument(f"--{flag_name}", **flag_info)
    args, _ = parser.parse_known_args()
    return args


def setup():
    global configArgs
    global projectName
    global projectPath
    global outputPath
    global maxNumWorkers
    global outFile
    global logger

    # setup console logger
    logger = logging.getLogger("test")
    logger.setLevel(level=getLogLevel())
    logStreamFormatter = getLoggingFormatter()
    consoleHandler = logging.StreamHandler(stream=sys.stdout)
    consoleHandler.setFormatter(logStreamFormatter)
    consoleHandler.setLevel(level=getLogLevel())
    logger.addHandler(consoleHandler)

    # get commandline arguments
    known, opts = get_cli_args(header)

    # print header
    print(header)

    # set project path
    projectPath = opts.project_path
    if os.path.exists(projectPath):
        projectPath = os.path.abspath(projectPath)
        config_file_path = f"{projectPath}/portfolio-config.json"
    else:
        raise FileNotFoundError(f"Unable to find project directory: {projectPath}")

    # set output path
    outputPath = f"{projectPath}/portfolio"

    # reset project output path
    if os.path.exists(outputPath):
        shutil.rmtree(outputPath)
    os.makedirs(outputPath)

    # add file logger
    outFile = f"{projectPath}/portfolio.out"
    open(outFile, 'w')
    logFileFormatter = getLoggingFormatter()
    fileHandler = logging.FileHandler(filename=outFile)
    fileHandler.setFormatter(logFileFormatter)
    fileHandler.setLevel(level=getLogLevel())
    logger.addHandler(fileHandler)
    logger.info(f"Logging in file {outFile}")

    # check if portfolio-config.json file exists
    if not os.path.isfile(f"{config_file_path}"):
        raise FileNotFoundError(f"Unable to find portfolio-config.json file in {config_file_path}")

    maxNumWorkers = opts.nproc
    configArgs = get_config_args(config_file_path)

    # set project name
    assert(str(configArgs.pproj).endswith(".pproj"))
    projectName = str(configArgs.pproj)[:-6]
    logger.info(f"Project name: {projectName}")
    logger.info(f"Project path: {projectPath}")
    logger.info(f"Output path: {outputPath}")
    logger.info(f"Number of workers per method: {configArgs.partitions}")


def run_compiler_csharp():
    global projectName
    global dllFile
    global PC

    # change directory to input path
    os.chdir(projectPath)

    # check if .pproj file exists
    if not os.path.isfile(f"{configArgs.pproj}"):
        raise FileNotFoundError(f"Compilation error: Unable to find .pproj file {configArgs.pproj}")

    logger.info(f"Using P compiler: {PC}")

    # run p compiler for csharp
    cmd = PC.split(" ")
    cmd.append(f"-proj:{configArgs.pproj}")
    cmd.append("-generate:CSharp")
    cmd_str = " ".join(cmd)
    logger.debug(f"Compiling P model for C# with command: {cmd_str}")

    try:
        subprocess.run(cmd, check=True)
    except subprocess.SubprocessError:
        logger.error("Failed to compile pproj")
        raise

    # set dll file
    dllFile = f"{projectPath}/POutput/netcoreapp3.1/{projectName}.dll"

    # check if dll file exists
    if not os.path.isfile(dllFile):
        raise FileNotFoundError(f"Compilation error: Unable to find .dll file {dllFile}")

    logger.info("Compilation successful.")
    logger.info(f"Generated C# target: {dllFile}")


def run_compiler_psym():
    global projectName
    global psymJarFile
    global PC

    # change directory to input path
    os.chdir(projectPath)

    # check if .pproj file exists
    if not os.path.isfile(f"{configArgs.pproj}"):
        raise FileNotFoundError(f"Compilation error: Unable to find .pproj file {configArgs.pproj}")

    logger.info(f"Using P compiler: {PC}")

    # run p compiler for psym
    cmd = PC.split(" ")
    cmd.append(f"-proj:{configArgs.pproj}")
    cmd.append("-generate:PSym")
    cmd_str = " ".join(cmd)
    logger.debug(f"Compiling P model for PSym with command: {cmd_str}")

    try:
        subprocess.run(cmd, check=True)
    except subprocess.SubprocessError:
        logger.error("Failed to compile pproj")
        raise

    # set psym jar file
    psymJarFile = f"{projectPath}/target/{projectName}-jar-with-dependencies.jar"

    # check if psym jar file exists
    if not os.path.isfile(psymJarFile):
        raise FileNotFoundError(f"Compilation error: Unable to find PSym .jar file {psymJarFile}")

    logger.info("Compilation successful.")
    logger.info(f"Generated PSym target: {psymJarFile}")


def initialize_coyote_all():
    # change directory to output path
    os.chdir(outputPath)

    num_initialized = 0
    methods = []

    if (not hasattr(configArgs, "methods")) or (not configArgs.methods):
        methods.append("")
    else:
        methods = [method for method in configArgs.methods]

    for methodWorker in range(configArgs.partitions):
        for method in methods:
            initialize_coyote_worker(method)
            num_initialized += 1
    logger.info(f"Initialized {num_initialized} Coyote workers")


def initialize_psym_all():
    # change directory to output path
    os.chdir(outputPath)

    num_initialized = 0
    methods = []

    if (not hasattr(configArgs, "methods")) or (not configArgs.methods):
        methods.append("")
    else:
        methods = [method for method in configArgs.methods]

    for mode in psymModes:
        for method in methods:
            initialize_psym_worker(method, mode)
            num_initialized += 1
    logger.info(f"Initialized {num_initialized} PSym workers")


def initialize_coyote_worker(method):
    global dllFile

    worker = Worker(method, "coyote")
    mode, schedule = choose_strategy(int(worker.get_id()))
    worker.set_path(mode)
    cmd = ["coyote test", dllFile, schedule, "--graph", "--coverage activity", f"--outdir {worker.get_path()}"]
    if method != "":
        cmd.append("-m " + method)
    if hasattr(configArgs, "iterations"):
        cmd.append("-i " + str(configArgs.iterations))
    elif hasattr(configArgs, "search_depth"):
        cmd.append("-i " + str(configArgs.search_depth))
    if hasattr(configArgs, "max_steps"):
        cmd.append("--max-steps " + str(configArgs.max_steps))
    if hasattr(configArgs, "timeout"):
        cmd.append("--timeout " + str(configArgs.timeout))
    if hasattr(configArgs, "verbose"):
        if configArgs.verbose:
            cmd.append("-v")
    cmd_str = " ".join(cmd)
    worker.set_cmd(cmd_str)
    worker.set_status("initialized")
    allWorkers.append(worker)
    pendingWorkers.append(worker)

    logger.debug(f"Initialized Coyote {worker} with command: {cmd_str}")


def initialize_psym_worker(method, mode):
    global psymJarFile

    worker = Worker(method, "psym")
    worker.set_path(mode)
    cmd = ["java -jar", psymJarFile, f"-p {worker}", f"--outdir {worker.get_path()}", f"--mode {mode}"]
    if method != "":
        cmd.append("-m " + f"{method_pretty_name(method)}")
    if hasattr(configArgs, "iterations"):
        cmd.append("-i " + str(configArgs.iterations))
    if hasattr(configArgs, "max_steps"):
        cmd.append("--max-steps " + str(configArgs.max_steps))
    if hasattr(configArgs, "timeout"):
        cmd.append("-tl " + str(configArgs.timeout))
    if hasattr(configArgs, "verbose"):
        if configArgs.verbose:
            cmd.append("-v 1")
    cmd_str = " ".join(cmd)
    worker.set_cmd(cmd_str)
    worker.set_status("initialized")
    allWorkers.append(worker)
    pendingWorkers.insert(0, worker)

    logger.debug(f"Initialized PSym {worker} with command: {cmd_str}")

def spawn_worker(worker):
    assert(worker.get_status() == "initialized")
    worker.set_status("running")

    # set worker output path and stdout/stderr files
    os.makedirs(worker.get_path())
    out_file = open(f"{worker.get_path()}/run.out", "w")
    err_file = open(f"{worker.get_path()}/run.err", "w")

    proc = subprocess.Popen("exec " + worker.get_cmd(), shell=True, stdout=out_file, stderr=err_file)
    processes[worker] = proc
    runningWorkers.add(worker)

    logger.info(f"Started {worker} with pid {proc.pid}")


def check_workers_all():
    newly_completed_workers = []
    for worker in runningWorkers:
        assert(worker.get_status() == "running")
        if worker.get_category() == "coyote":
            check_coyote_worker(worker)
        elif worker.get_category() == "psym":
            check_psym_worker(worker)
        else:
            logger.error(f"Unrecognized worker of type: {worker.get_category()}")
            sys.exit(1)
        if worker.get_status() != "running":
            newly_completed_workers.append(worker)
            logger.info(f"{worker} finished with result: {worker.get_status()}")
    for worker in newly_completed_workers:
        runningWorkers.remove(worker)
        completedWorkers.add(worker)


def check_coyote_worker(worker):
    proc = processes[worker]
    ret_code = proc.poll()
    if ret_code is not None:
        worker_status = "unknown"
        worker_stdout = f"{worker.get_path()}/run.out"
        if os.path.isfile(worker_stdout):
            with open(worker_stdout) as f:
                worker_log = f.read()
                if 'Found 0 bugs' in worker_log:
                    worker_status = "Found 0 bugs"
                elif 'found a bug.' in worker_log:
                    worker_status = "Found a bug"
                    buggyWorkers.add(worker)
        worker.set_status(worker_status)


def check_psym_worker(worker):
    proc = processes[worker]
    ret_code = proc.poll()
    if ret_code is not None:
        worker_status = "unknown"
        worker_stdout = f"{worker.get_path()}/stats-{worker}.log"
        if os.path.isfile(worker_stdout):
            with open(worker_stdout) as f:
                for line in f.readlines()[0:]:
                    entry = line.split(':', 1)
                    if entry:
                        lhs = entry[0]+":"
                        if lhs == "status:":
                            assert (len(entry) == 2)
                            worker_status = entry[1].lstrip().rstrip()
                            if worker_status == "success":
                                provedWorkers.add(worker)
                            elif worker_status == "cex":
                                buggyWorkers.add(worker)
        worker.set_status(worker_status)


def terminate_all():
    logger.info("Stopping all workers")
    for worker in processes.keys():
        terminate(worker)


def terminate(worker):
    proc = processes[worker]
    if proc.poll() is None:
        proc.terminate()
        proc.kill()
        if worker in runningWorkers:
            runningWorkers.remove(worker)


def choose_strategy(number):
    """
    Choose a scheduling strategy based on the array job index
    """
    if (number + 1) % 2 == 0:
        return "random", "--sch-random"

    if number % 4 == 0:
        return "pct", f"--sch-pct {number}"

    return "fairpct", f"--sch-fairpct {number}"


def method_pretty_name(method):
    result = method
    if result.startswith("PImplementation."):
        result = result[len("PImplementation."):]
    if result.endswith(".Execute"):
        result = result[0:len(result)-len(".Execute")]
    return result


def get_elapsed_time():
    return time.time() - startTime


def report_current_status():
    logger.info("-------")
    logger.info("Status after %d seconds:" % get_elapsed_time())
    logger.info("-------")
    logger.info(f"  total:     {len(allWorkers)}")
    logger.info(f"  completed: {len(completedWorkers)}")
    logger.info(f"  running:   {len(runningWorkers)}")
    logger.info(f"  pending:   {len(pendingWorkers)}")
    if buggyWorkers:
        logger.info(f"  buggy:     {len(buggyWorkers)} -- {str(buggyWorkers)}")
    else:
        logger.info(f"  buggy:     {len(buggyWorkers)}")
    if psymJarFile != "":
        if provedWorkers:
            logger.info(f"  proved:    {len(provedWorkers)} -- {str(provedWorkers)}")
        else:
            logger.info(f"  proved:    {len(provedWorkers)}")
    [h_weak_ref().flush() for h_weak_ref in logging._handlerList]

def getLoggingFormatter():
    return logging.Formatter(
        fmt=f"%(levelname)-8s %(asctime)s \t %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S")


def getLogLevel():
    return logging.INFO

def main():
    global dllFile
    global psymJarFile
    global configArgs
    setup()

    # set dll file
    if hasattr(configArgs, "dll"):
        dllFile = f"{projectPath}/{configArgs.dll}"
        if not os.path.isfile(dllFile):
            logger.info(f"Unable to find DLL file {dllFile}")
            logger.info("Recompiling for C#...")
            run_compiler_csharp()
        logger.info(f"DLL file: {dllFile}")

    # set psym jar file
    if hasattr(configArgs, "psym_jar"):
        psymJarFile = f"{projectPath}/{configArgs.psym_jar}"
        if not os.path.isfile(psymJarFile):
            logger.info(f"Unable to find PSym JAR file {psymJarFile}")
            logger.info("Recompiling for PSym...")
            run_compiler_psym()
        logger.info(f"PSym JAR file: {psymJarFile}")

    if dllFile != "":
        initialize_coyote_all()

    if psymJarFile != "":
        initialize_psym_all()

    while True:
        if runningWorkers:
            check_workers_all()
        num_spawned = 0
        while pendingWorkers and len(runningWorkers) < maxNumWorkers:
            worker = pendingWorkers.pop(0)
            spawn_worker(worker)
            num_spawned += 1
        if num_spawned:
            logger.info(f"Spawned {num_spawned} workers")
        report_current_status()
        if (not runningWorkers) and (not pendingWorkers):
            break
        if hasattr(configArgs, "polling_interval"):
            time.sleep(int(configArgs.polling_interval))
        else:
            time.sleep(10)

    logger.info("-------")

    logger.info(f"Check output in {outFile}")


if __name__ == '__main__':
    main()
