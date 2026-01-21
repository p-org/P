package pobserve.commons.utils;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.Iterator;
import java.util.Scanner;

public class LogBreaker implements Iterator<String> {
    private final Iterator<String> internalIterator;
    private final long delimiterByteCount;
    private final boolean supportByteCount;
    private long byteCount;

    public LogBreaker(String delimiter, InputStream inputStream) throws IOException {
        this(delimiter, inputStream, 0);
    }

    public LogBreaker(String delimiter, InputStream inputStream, long startingByteCount) throws IOException {
        byteCount = startingByteCount;
        String normalizedDelimiter = delimiter.replace("\\n", "\n");
        if (normalizedDelimiter.equals("\n")) {
            delimiterByteCount = 1;
            supportByteCount = true;
            internalIterator
                    = new BufferedReaderIterator(
                            new BufferedReader(
                                    new InputStreamReader(inputStream, StandardCharsets.UTF_8)));
        } else {
            String delimiterSubstring = normalizedDelimiter.substring(1, normalizedDelimiter.length() - 1);
            if (normalizedDelimiter.startsWith("\n") && normalizedDelimiter.endsWith("\n")
                    && !delimiterSubstring.contains("\n")) {
                delimiterByteCount = countUtf8Bytes(normalizedDelimiter) - 1;
                supportByteCount = true;
                String lineDelimiter = delimiterSubstring;
                internalIterator
                        = new MultilineIterator(
                        lineDelimiter,
                        new BufferedReader(
                                new InputStreamReader(inputStream, StandardCharsets.UTF_8)
                        ));
            } else {
                delimiterByteCount = 0;
                supportByteCount = false;
                Scanner scanner = new Scanner(inputStream, StandardCharsets.UTF_8);
                scanner.useDelimiter(delimiter);

                internalIterator = scanner;
            }
        }
    }

    @Override
    public boolean hasNext() {
        return internalIterator.hasNext();
    }

    @Override
    public String next() {
        String line = internalIterator.next();
        byteCount += delimiterByteCount + countUtf8Bytes(line);
        return line;
    }

    public boolean supportByteCount() {
        return supportByteCount;
    }

    public long getByteCount() {
        if (supportByteCount) {
            return byteCount;
        }

        throw new UnsupportedOperationException("This LogBreaker does not support byte counting");
    }

    private static int countUtf8Bytes(String str) {
        int byteCount = 0;
        for (int i = 0; i < str.length(); i++) {
            char c = str.charAt(i);
            if (c <= 0x7F) {
                // ASCII characters (0-127) take 1 byte in UTF-8
                byteCount += 1;
            } else if (c <= 0x7FF) {
                // Characters in range 128-2047 take 2 bytes in UTF-8
                byteCount += 2;
            } else if (Character.isHighSurrogate(c) && i + 1 < str.length() &&
                    Character.isLowSurrogate(str.charAt(i + 1))) {
                // Surrogate pairs (characters outside BMP) take 4 bytes in UTF-8
                byteCount += 4;
                i++; // Skip the low surrogate
            } else {
                // Other characters (including most non-Latin scripts) take 3 bytes
                byteCount += 3;
            }
        }
        return byteCount;
    }
}

class BufferedReaderIterator implements Iterator<String> {
    private final BufferedReader bufferedReader;
    private String line;

    BufferedReaderIterator(BufferedReader bufferedReader) {
        this.bufferedReader = bufferedReader;
        advance();
    }

    @Override
    public boolean hasNext() {
        return line != null;
    }

    @Override
    public String next() {
        String prevLine = line;
        advance();
        return prevLine;
    }

    private void advance() {
        try {
            line = bufferedReader.readLine();
        } catch (IOException ioe) {
            throw new RuntimeException(ioe);
        }
    }
}

class MultilineIterator implements Iterator<String> {
    private final String lineDelimiter;
    private final BufferedReader bufferedReader;

    private final StringBuilder buffer;
    private boolean eof;

    MultilineIterator(String lineDelimiter, BufferedReader bufferedReader) {
        this.lineDelimiter = lineDelimiter;
        this.bufferedReader = bufferedReader;
        this.buffer = new StringBuilder();
        eof = false;
        advance();
    }

    @Override
    public boolean hasNext() {
        return !eof;
    }

    @Override
    public String next() {
        String toReturn = buffer.toString();
        advance();
        return toReturn;
    }

    private void advance() {
        buffer.setLength(0);
        if (!eof) {
            try {
                while (true) {
                    String line = bufferedReader.readLine();
                    if (line == null) {
                        eof = true;
                        return;
                    } else if (line.equals(lineDelimiter)) {
                        return;
                    } else {
                        buffer.append(line);
                        buffer.append('\n');
                    }
                }
            } catch (IOException ioe) {
                throw new RuntimeException(ioe);
            }
        }
    }
}

