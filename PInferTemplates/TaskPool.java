import java.nio.file.Paths;
import java.nio.file.Files;
import java.util.concurrent.TimeUnit;

public class TaskPool {
    int ptr = 0;
    int chunkSize;
    int running;
    int numFinished;
    int numMined;
    int numTasks;
    List<Task> tasks;
    Map<String, Map<String, Integer>> headerToNumTasks;
    Map<String, Map<String, List<Task>>> taskIndex;
    final FromDaikon converter;
    final boolean verbose;
    final BufferedOutputStream pinferOutputStream;
    final BufferedOutputStream pinferParsableStream;
    final BufferedOutputStream pinferParsableAllStream;
    final long startTime;
    final File outputFile;
    static final File DaikonTracesDir = new File("tmp_daikon_traces");

    public TaskPool(int chunkSize, FromDaikon converter, String filename, String outputDir, boolean verbose) throws IOException {
        System.out.println("Running with " + chunkSize + " cores");
	this.chunkSize = chunkSize;
        this.running = 0;
        this.tasks = new ArrayList<>();
        this.headerToNumTasks = new HashMap<>();
        this.taskIndex = new HashMap<>();
        this.ptr = 0;
        this.numFinished = 0;
        this.numMined = 0;
        this.converter = converter;
        this.verbose = verbose;
        this.numTasks = 0;
        File outputsParent = new File("PInferOutputs");
        File pinferOutputFileDir = new File(String.valueOf(Paths.get(outputsParent.toString(), outputDir)));
        if (!pinferOutputFileDir.isDirectory()) {
            pinferOutputFileDir.mkdirs();
        }
        File pinferOutputFile = new File(String.valueOf(Paths.get(pinferOutputFileDir.toString(), filename)));
        File pinferParsable = new File(String.valueOf(Paths.get(pinferOutputFileDir.toString(), "%PARSEFILE%")));
        File pinferParsableAll = new File(String.valueOf(Paths.get(pinferOutputFileDir.toString(), "all_%PARSEFILE%")));
        assert pinferOutputFile.createNewFile() : "Failed to create invariant output file " + pinferOutputFile;
        assert pinferParsable.createNewFile() : "Failed to create pinfer parsable file " + "%PARSEFILE%";
        assert pinferParsableAll.createNewFile() : "Failed to create pinfer parsable file " + "all_%PARSEFILE%";
        this.outputFile = pinferOutputFile;
        this.pinferOutputStream = new BufferedOutputStream(new FileOutputStream(pinferOutputFile));
        this.pinferParsableStream = new BufferedOutputStream(new FileOutputStream(pinferParsable));
        this.pinferParsableAllStream = new BufferedOutputStream(new FileOutputStream(pinferParsableAll, true));
        if (!DaikonTracesDir.exists()) {
            DaikonTracesDir.mkdirs();
        }
        this.startTime = System.currentTimeMillis();
    }

    public void addTask(Task task) throws IOException {
        String guardsStr = task.guardsStr();
        String filtersStr = task.filtersStr();
        if (!headerToNumTasks.containsKey(guardsStr)) {
            headerToNumTasks.put(guardsStr, new HashMap<>());
            this.taskIndex.put(guardsStr, new HashMap<>());
        }
        if (!headerToNumTasks.get(guardsStr).containsKey(filtersStr)) {
            headerToNumTasks.get(guardsStr).put(filtersStr, 0);
            this.taskIndex.get(guardsStr).put(filtersStr, new ArrayList<>());
        }
        int numTasks = headerToNumTasks.get(guardsStr).get(filtersStr);
        headerToNumTasks.get(guardsStr).put(filtersStr, numTasks + 1);
        this.taskIndex.get(guardsStr).get(filtersStr).add(task);
        tasks.add(task);
        numTasks += 1;
        _startTasks();
    }

    private void showProgress() {
        StringBuilder sb = new StringBuilder();
        sb.append("[").append(numFinished).append(" / ").append(tasks.size()).append("][");
        int total = 50;
        double percentage = (double) numFinished / tasks.size();
        int numEq = (int)(total * percentage);
        numEq = Math.max(numEq, 1);
        sb.append(String.join("", Collections.nCopies(numEq, "="))).append(">");
        sb.append(String.join("", Collections.nCopies(total - numEq, " "))).append("]").append("\r");
        System.out.print(sb);
    }

    private void showResults(String guards, String filters) throws InterruptedException, IOException {
        synchronized(this) {
            showProgress();
            String header = converter.getFormulaHeader(guards, filters);
            Set<String> invariants = new HashSet<>();
            for (var task : taskIndex.get(guards).get(filters)) {
                var result = task.getDaikonOutput(converter, verbose);
                if (result != null) {
                    invariants.addAll(result);
                }
            }
            if (!invariants.isEmpty()) {
                String body = String.join("\n", invariants);
                pinferOutputStream.write((header + "\n" + body + "\n\n").getBytes());
                // parsable output in the following format
                // guards*
                // filters*
                // properties*
                // separated by \wedge
                pinferParsableStream.write((guards + "\n").getBytes());
                pinferParsableStream.write((filters + "\n").getBytes());
                pinferParsableStream.write((String.join(" ∧ ", invariants) + "\n").getBytes());
                // store all results in a single file
                pinferParsableAllStream.write((guards + "\n").getBytes());
                pinferParsableAllStream.write((filters + "\n").getBytes());
                pinferParsableAllStream.write((String.join(" ∧ ", invariants) + "\n").getBytes());
                this.numMined += 1;
            }
            notify();
        }
    }

    public synchronized int getNumMined() {
      return this.numMined;
    }

    public void notifyFinished(Task t) throws IOException, InterruptedException {
        synchronized (this) {
            assert running > 0;
            running -= 1;
            numFinished += 1;
            _startTasks();
            String guardsStr = t.guardsStr();
            String filtersStr = t.filtersStr();
            int tasksRemain = this.headerToNumTasks.get(guardsStr).get(filtersStr) - 1;
            this.headerToNumTasks.get(guardsStr).put(filtersStr, tasksRemain);
            if (tasksRemain == 0) {
                showResults(guardsStr, filtersStr);
            }
        }
    }

    public void waitForAll() throws InterruptedException, IOException {
        synchronized (this) {
            showProgress();
            if (numFinished < tasks.size()) {
                while (numFinished < tasks.size()) {
                    wait(0);
                }
            }
            System.out.println();
            System.out.println("Time used (seconds): " + (double)(System.currentTimeMillis() - startTime) / 1000.0);
            System.out.println("#Properties mined: " + numMined);
            System.out.println("Output to " + outputFile.getPath());
            pinferParsableAllStream.write((numFinished + "\n").getBytes());
            pinferParsableAllStream.write(("EOT\n").getBytes());
            pinferParsableStream.write((numFinished + "").getBytes());
            pinferOutputStream.flush();
            pinferParsableStream.flush();
            pinferParsableAllStream.flush();
            pinferOutputStream.close();
            pinferParsableStream.close();
            pinferParsableAllStream.close();
        }
    }

    public synchronized void _startTasks() throws IOException {
        while (running < chunkSize && ptr < tasks.size()) {
            tasks.get(ptr++).start(ptr);
            running += 1;
        }
    }

    public static class Task {

        private final List<String> tracePaths;
        private final List<Main.RawPredicate> guards;
        private final List<Main.RawTerm> forallQuantifiedTerms;
        private final List<Main.RawTerm> existsQuantifiedTerms;
        private final List<Main.RawPredicate> filters;
        private String templateName;
        private final StringBuilder daikonOutput;
        private final StringBuilder daikonStdErr;
        private Process runningProg;
        private Thread outputThread;
        private final TaskPool poolRef;
        private final String templatePrefix;
        private final String guardsStr;
        private final String filtersStr;
        private final int numForall;
        private final int numExists;

        public Task(List<String> tracePaths,
                    int numForall, int numExists,
                    List<Main.RawPredicate> guards,
                    List<Main.RawPredicate> filters,
                    List<Main.RawTerm> forallQuantifiedTerms,
                    List<Main.RawTerm> existsQuantifiedTerms,
                    String templatePrefix,
                    TaskPool poolRef) {
            this.tracePaths = tracePaths;
            this.templatePrefix = templatePrefix;
            this.guards = guards;
            this.guardsStr = guards.stream().map(Main.RawPredicate::shortRepr).collect(Collectors.joining(" ∧ "));;
            this.forallQuantifiedTerms = forallQuantifiedTerms.stream().sorted(Comparator.comparing(Main.RawTerm::type)).toList();
            this.existsQuantifiedTerms = existsQuantifiedTerms.stream().sorted(Comparator.comparing(Main.RawTerm::type)).toList();
            this.filters = filters;
            this.filtersStr = filters.stream().map(Main.RawPredicate::shortRepr).collect(Collectors.joining(" ∧ "));
            this.daikonOutput = new StringBuilder();
            this.daikonStdErr = new StringBuilder();
            this.runningProg = null;
            this.outputThread = null;
            this.poolRef = poolRef;
            this.numForall = numForall;
            this.numExists = numExists;
        }

        public String guardsStr() {
            return guardsStr;
        }

        public String filtersStr() {
            return filtersStr;
        }

        public Set<String> getDaikonOutput(FromDaikon converter, boolean verbose) throws InterruptedException {
            assert this.outputThread != null;
            String result = daikonOutput.toString();
            String prop;
            String[] lines = result.split("\n");
            Set<String> properties = new HashSet<>();
            boolean start = false;
            boolean hasResult = false;
            for (String line : lines) {
                if (line.contains(":::ENTER") && line.contains("mine_" + templateName)) {
                    start = true;
                    hasResult = true;
                    continue;
                }
                if (line.contains("=====") && start) break;
                if (start && (prop = converter.convertOutput(line, guards, filters, forallQuantifiedTerms, existsQuantifiedTerms)) != null) {
                    properties.add(prop);
                }
            }
            var stderr = daikonStdErr.toString().trim();
            if (verbose && stderr.contains("Exception")) {
                System.err.println("Exception raised: " + stderr);
                return properties;
            }
            if (verbose && stderr.contains("Unknown template")) {
                System.err.println("Skipping unknown template: " + stderr);
                return properties;
            }
            if (!hasResult && daikonStdErr.toString().contains("No program point declarations were found.")) {
                return null;
            }
            return properties;
        }

        public void kill() {
            if (runningProg != null) {
                runningProg.destroy();
            }
        }

        public String showTask() {
            StringBuilder builder = new StringBuilder();
            builder.append("Template: ").append(templateName).append("\n");
            builder.append("Guards: ").append(guards.stream().map(Main.RawPredicate::repr).collect(Collectors.joining(" "))).append("\n");
            builder.append("filters: ").append(filters.stream().map(Main.RawPredicate::repr).collect(Collectors.joining(" "))).append("\n");
            builder.append("Terms (forall): ").append(forallQuantifiedTerms.stream().map(Main.RawTerm::shortRepr).collect(Collectors.joining(" "))).append("\n");
            builder.append("Terms (exists): ").append(existsQuantifiedTerms.stream().map(Main.RawTerm::shortRepr).collect(Collectors.joining(" "))).append("\n");
            builder.append("Current Time: ").append(System.currentTimeMillis());
            return builder.toString();
        }

        private void watch() {
            outputThread = new Thread(() -> {
                BufferedReader reader = new BufferedReader(new InputStreamReader(runningProg.getInputStream()));
                String line;
                while (true) {
                    try {
                        if ((line = reader.readLine()) == null) {
                            break;
                        }
                        daikonOutput.append(line).append("\n");
                    } catch (IOException e) {
                        throw new RuntimeException(e);
                    }
                }
                reader = new BufferedReader(new InputStreamReader(runningProg.getErrorStream()));
                while (true) {
                    try {
                        if ((line = reader.readLine()) == null) {
                            break;
                        }
                        daikonStdErr.append(line).append("\n");
                    } catch (IOException e) {
                        throw new RuntimeException(e);
                    }
                }
                try {
                    poolRef.notifyFinished(this);
                } catch (IOException | InterruptedException e) {
                    throw new RuntimeException(e);
                }
            });
            outputThread.start();
        }

        public void start(int uid) throws IOException {
            StringBuilder templateNameBuilder = new StringBuilder();
            StringBuilder forallTypes = new StringBuilder();
            for (var t: forallQuantifiedTerms) {
                forallTypes.append(t.type());
            }
            StringBuilder existsTypes = new StringBuilder();
            for (var t: existsQuantifiedTerms) {
                existsTypes.append(t.type());
            }
            templateNameBuilder.append("Forall").append(numForall).append(forallTypes).append("Exists").append(numExists).append(existsTypes);
            this.templateName = templateNameBuilder.toString().strip();
            ProcessBuilder pb = new ProcessBuilder("java",
                    "-Xmx32g",
                    "-cp",
                    System.getProperty("java.class.path"),
                    "daikon.Chicory",
                    "--ppt-select-pattern=" + "Templates",
                    "--ppt-omit-pattern=execute",
                    "--dtrace-file=" + Paths.get("tmp_daikon_traces", Integer.toHexString(uid) + ".dtrace.gz").toString(),
                    "--daikon",
                    "%PROJECT_NAME%.pinfer.PInferDriver",
                    String.join("*", tracePaths),
                    templateName,
                    guards.stream().map(Main.RawPredicate::repr).collect(Collectors.joining("@@")),
                    filters.stream().map(Main.RawPredicate::repr).collect(Collectors.joining("@@")),
                    forallQuantifiedTerms.stream().map(Main.RawTerm::repr).collect(Collectors.joining("@@")),
                    existsQuantifiedTerms.stream().map(Main.RawTerm::repr).collect(Collectors.joining("@@")));
            runningProg = pb.start();
            watch();
        }
    }
}
