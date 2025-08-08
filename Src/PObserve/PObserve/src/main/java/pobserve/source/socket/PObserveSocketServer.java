package pobserve.source.socket;

import pobserve.logger.PObserveLogger;
import pobserve.source.socket.models.Request;
import pobserve.source.socket.models.Response;
import pobserve.source.socket.utils.ErrorHandler;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.io.PrintWriter;
import java.net.ServerSocket;
import java.net.Socket;
import java.net.SocketException;
import java.nio.charset.StandardCharsets;
import java.util.Locale;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;


/**
 * Socket server for PObserve that implements a socket API for log processing.
 * Supports both JSON and TEXT formats based on the --inputkind parameter.
 * Utilizes a thread pool to efficiently handle multiple concurrent client connections.
 *
 * Supports commands:
 * - START: Begin monitoring
 * - RESET: Reset the P monitor state
 * - ACCEPT: Process a log line
 * - STOP: Stop monitoring and print summary
 */
public class PObserveSocketServer {
    private final int port;
    private final String host;
    private ServerSocket serverSocket;
    private final AtomicBoolean running = new AtomicBoolean(false);
    private final SocketCommandProcessor commandProcessor;
    private final SocketResponseFormatter responseFormatter;
    private final ObjectMapper objectMapper;
    private ExecutorService threadPool;
    // Default number of threads in the pool, can be made configurable
    private static final int DEFAULT_THREAD_POOL_SIZE = 10;

    /**
     * Creates a new socket server for PObserve.
     *
     * @param host The host to bind to
     * @param port The port to listen on
     */
    public PObserveSocketServer(String host, int port) {
        this.host = host;
        this.port = port;
        this.commandProcessor = new SocketCommandProcessor();

        this.objectMapper = new ObjectMapper()
            .enable(SerializationFeature.INDENT_OUTPUT)
            .setSerializationInclusion(JsonInclude.Include.NON_NULL);

        this.responseFormatter = new SocketResponseFormatter(objectMapper);
    }

    /**
     * Starts the server on the configured host and port.
     */
    public void start() {
        PObserveLogger.info("Starting PObserve socket server on " + host + ":" + port);
        running.set(true);

        // Initialize thread pool for client connections
        threadPool = Executors.newFixedThreadPool(DEFAULT_THREAD_POOL_SIZE);
        PObserveLogger.info("Thread pool initialized with " + DEFAULT_THREAD_POOL_SIZE + " threads");

        try {
            serverSocket = new ServerSocket(port);
            PObserveLogger.info("PObserve socket server started successfully");

            while (running.get()) {
                try {
                    Socket clientSocket = serverSocket.accept();
                    PObserveLogger.info("Client connected from " + clientSocket.getInetAddress());
                    handleClient(clientSocket);
                } catch (SocketException e) {
                    if (running.get()) {
                        PObserveLogger.error("Socket exception: " + e.getMessage());
                    }
                } catch (IOException e) {
                    PObserveLogger.error("Error accepting client connection: " + e.getMessage());
                }
            }
        } catch (IOException e) {
            PObserveLogger.error("Error starting socket server: " + e.getMessage());
            pobserve.report.TrackErrors.addError(new pobserve.report.PObserveError(e));
        }
    }

    /**
     * Stops the server.
     */
    public void stop() {
        PObserveLogger.info("Stopping PObserve socket server");
        running.set(false);

        // Shut down the thread pool
        if (threadPool != null && !threadPool.isShutdown()) {
            PObserveLogger.info("Shutting down thread pool");
            threadPool.shutdown();
            try {
                // Wait for existing tasks to terminate
                if (!threadPool.awaitTermination(30, TimeUnit.SECONDS)) {
                    PObserveLogger.warn("Thread pool did not terminate in the specified time");
                    // Cancel currently executing tasks
                    threadPool.shutdownNow();
                    // Wait a while for tasks to respond to being cancelled
                    if (!threadPool.awaitTermination(30, TimeUnit.SECONDS)) {
                        PObserveLogger.error("Thread pool did not terminate");
                    }
                }
            } catch (InterruptedException e) {
                // (Re-)Cancel if current thread also interrupted
                threadPool.shutdownNow();
                // Preserve interrupt status
                Thread.currentThread().interrupt();
                PObserveLogger.error("Thread pool shutdown interrupted: " + e.getMessage());
            }
        }

        // Close the server socket
        if (serverSocket != null && !serverSocket.isClosed()) {
            try {
                serverSocket.close();
            } catch (IOException e) {
                PObserveLogger.error("Error closing server socket: " + e.getMessage());
            }
        }
    }

    /**
     * Handles a client connection using the thread pool.
     *
     * @param clientSocket The client socket
     */
    private void handleClient(Socket clientSocket) {
        threadPool.execute(() -> {
            try (
                BufferedReader reader = new BufferedReader(new InputStreamReader(clientSocket.getInputStream(), StandardCharsets.UTF_8));
                PrintWriter writer = new PrintWriter(new OutputStreamWriter(clientSocket.getOutputStream(), StandardCharsets.UTF_8), true)
            ) {
                // Check the input kind from the config
                boolean isJsonMode = pobserve.config.PObserveConfig.getPObserveConfig().getInputKind()
                    == pobserve.config.SourceInputKind.JSON;

                // Send initial welcome message
                responseFormatter.sendResponse(writer, isJsonMode, SocketCommandProcessor.CMD_START,
                    "PObserve socket server ready. Available commands: START, RESET, ACCEPT, STOP", null, true);

                String line;
                while (running.get() && (line = reader.readLine()) != null) {
                    processCommand(line, writer, isJsonMode);
                }
            } catch (IOException e) {
                PObserveLogger.error("Error handling client: " + e.getMessage());
            } finally {
                try {
                    clientSocket.close();
                } catch (IOException e) {
                    PObserveLogger.error("Error closing client socket: " + e.getMessage());
                }
            }
        });
    }

    /**
     * Process a command and send the appropriate response.
     *
     * @param commandLine The command line to process
     * @param writer The output writer
     * @param isJsonMode Whether the command should be processed as JSON or TEXT
     */
    private void processCommand(String commandLine, PrintWriter writer, boolean isJsonMode) {
        String command = null;
        String logLine = null;

        // Parse the command based on format
        if (isJsonMode) {
            try {
                Request request = objectMapper.readValue(commandLine, Request.class);
                command = request.getCommand();
                logLine = request.getLogLine();
            } catch (JsonProcessingException e) {
                PObserveLogger.error("Error parsing JSON: " + e.getMessage());
                responseFormatter.sendResponse(
                    writer, isJsonMode, null,
                    "Invalid JSON format: " + e.getMessage(),
                    null, false
                );
                return;
            }
        } else {
            if (commandLine.equalsIgnoreCase(SocketCommandProcessor.CMD_START)) {
                command = SocketCommandProcessor.CMD_START;
            } else if (commandLine.equalsIgnoreCase(SocketCommandProcessor.CMD_RESET)) {
                command = SocketCommandProcessor.CMD_RESET;
            } else if (commandLine.equalsIgnoreCase(SocketCommandProcessor.CMD_STOP)) {
                command = SocketCommandProcessor.CMD_STOP;
            } else if (commandLine.toUpperCase(Locale.ROOT).startsWith(SocketCommandProcessor.CMD_ACCEPT + " ")) {
                command = SocketCommandProcessor.CMD_ACCEPT;
                logLine = commandLine.substring(SocketCommandProcessor.CMD_ACCEPT.length() + 1); // Remove 'ACCEPT ' prefix
            } else {
                responseFormatter.sendResponse(writer, isJsonMode, null,
                    "Unknown command: " + commandLine + ". Available commands: START, RESET, ACCEPT <log_line>, STOP",
                    null, false);
                return;
            }
        }

        // Execute the command
        executeCommand(writer, isJsonMode, command, logLine);
    }

    /**
     * Execute a command and send the appropriate response.
     *
     * @param writer The output writer
     * @param isJsonMode Whether to use JSON or TEXT format for the response
     * @param command The command to execute
     * @param logLine The log line to process (for ACCEPT command)
     */
    private void executeCommand(PrintWriter writer, boolean isJsonMode, String command, String logLine) {
        Response response;

        try {
            if (command == null) {
                response = new Response("ERROR", "Missing command", null);
            } else {
                switch (command.toUpperCase(Locale.ROOT)) {
                    case SocketCommandProcessor.CMD_START:
                        response = commandProcessor.handleStartCommand();
                        break;
                    case SocketCommandProcessor.CMD_RESET:
                        response = commandProcessor.handleResetCommand();
                        break;
                    case SocketCommandProcessor.CMD_ACCEPT:
                        if (logLine == null || logLine.trim().isEmpty()) {
                            response = new Response("ERROR", "Missing log line for ACCEPT command", null);
                        } else {
                            response = commandProcessor.handleAcceptCommand(logLine);
                        }
                        break;
                    case SocketCommandProcessor.CMD_STOP:
                        response = commandProcessor.handleStopCommand();
                        break;
                    default:
                        response = new Response(
                            "ERROR",
                            "Unknown command: " + command +
                            ". Available commands: START, RESET, ACCEPT, STOP",
                            null
                        );
                        break;
                }
            }
        } catch (Exception e) {
            PObserveLogger.error("Error processing command: " + e.getMessage() + "\n" + ErrorHandler.getStackTraceAsString(e));
            response = new Response("ERROR", "Exception processing command: " + e.getMessage(), null);
        }

        // Send the response
        responseFormatter.sendResponse(writer, isJsonMode, command, response.getMessage(), response.getData(), "SUCCESS".equals(response.getStatus()));
    }
}
