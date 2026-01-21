!!! check ""
    Before moving forward, we assume that you have successfully installed
    the [PVerifier](install-pverifier.md).

In this section, we provide an overview of the steps involved in verifying a P program using the [two-phase commit](../../tutorial/twophasecommit.md) example in Tutorials.

??? info "Get the Two-Phase Commit Example Locally"
    We will use the [TwoPhaseCommit](https://github.com/p-org/P/tree/master/Tutorial/2_TwoPhaseCommit) example from Tutorial folder in P repository to describe the process of verifying a P program. Please clone the P repo and navigate to the
    TwoPhaseCommit example in Tutorial.

    Clone P Repo locally:
    ```shell
    git clone https://github.com/p-org/P.git
    ```
    Navigate to the TwoPhaseCommit examples folder:
    ```shell
    cd <P cloned folder>/Tutorial/2_TwoPhaseCommit
    ```

### Verifying a P program

To verify a P program using the PVerifier, you need to:

1. Configure your project to use PVerifier as the target in your `.pproj` file
2. Compile the project using the P compiler

This process follows the same workflow described in [Using P](../../getstarted/usingP.md), except that we specify `PVerifier` as the backend instead of other targets like `CSharp` or `Java`.

#### Executing the verification

After setting the target to `PVerifier` in your project file, run the P compiler with the following command:

```shell
p compile
```

The compiler will generate verification code and automatically invoke the PVerifier to check your model against the specifications defined in your P program. The verification results will be displayed in the terminal, showing whether the properties are satisfied or if there are any violations along with counterexample traces.

Running the verification engine on the Two-Phase Commit example will produce the following.

```
🎉 Verified 20 invariants!
✅ system_config_one_coordinator
✅ system_config_participant_set
✅ system_config_never_commit_to_coordinator
✅ system_config_never_abort_to_coordinator
✅ system_config_never_req_to_coordinator
✅ system_config_never_yes_to_participant
✅ system_config_never_yes_to_init
✅ system_config_never_no_to_participant
✅ system_config_never_no_to_init
✅ system_config_req_implies_not_init
✅ kondo_a1a
✅ kondo_a1b
✅ kondo_a2a
✅ kondo_a2b
✅ kondo_a3b
✅ kondo_a3a
✅ kondo_a4
✅ kondo_a5
✅ kondo_a6
✅ safety
✅ default P proof obligations
```
