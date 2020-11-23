This folder contains end-to-end examples.

To run the examples, invoke `run_test.py` in each subfolder.

Integrating the monitor with maven has the following steps:

1. Create the `monitor` folder under the project root directory and place the P specification under the `monitor` folder.
2. Update the `pom.xml`. We will use the followig 3 plugins:
    - exec-maven-plugin: invoke `gen_monitor.py` script to generate AspectJ and monitor code.
    - build-helper-maven-plugin: add monitor code to the project source code.
    - aspectj-maven-plugin: invoke aspectj during the compliation of source code and tests.
3. Place `gen_monitor.py` under the project root directoy. The script does the following things in order
    - Call P-Compiler on the P specification to generate a rvm spec and an AspectJ template. (see `translate` function)
    - Fill the AspectJ template and copy the result to `target/generated-sources/aspectJ`. (see `fillAspect` function)
    - Call Rv-Monitor on the rvm spec to generate the java monitor library and copy the code to `target/generated-sources/java`. (see `runMonitor` function)
    - Copy all the runtime to `target/generated-sources/java`. (see `copyRuntime` function)
4. Run `mvn test -Dtest=...` to run the tests with monitor.
