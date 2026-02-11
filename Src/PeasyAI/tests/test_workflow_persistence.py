import sys
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

PROJECT_ROOT = Path(__file__).parent.parent
SRC_ROOT = PROJECT_ROOT / "src"
sys.path.insert(0, str(SRC_ROOT))

from core.workflow.engine import WorkflowEngine, WorkflowDefinition
from core.workflow.events import EventEmitter
from core.workflow.steps import WorkflowStep, StepResult


class NeedsGuidanceStep(WorkflowStep):
    name = "needs_guidance"
    description = "Pause until user guidance is provided"
    max_retries = 1

    def execute(self, context):
        if context.get("user_guidance"):
            return StepResult.success({"guided_value": context["user_guidance"]})
        return StepResult.needs_guidance("Please provide guidance")

    def can_skip(self, context):
        return False


class FinishStep(WorkflowStep):
    name = "finish"
    description = "Finalize workflow"
    max_retries = 1

    def execute(self, context):
        return StepResult.success({"done": True})

    def can_skip(self, context):
        return False


class TestWorkflowPersistence(unittest.TestCase):
    def test_pause_resume_persists_state(self):
        with TemporaryDirectory() as tmpdir:
            state_file = str(Path(tmpdir) / "workflow_state.json")

            emitter = EventEmitter()
            engine = WorkflowEngine(emitter, state_store_path=state_file)
            workflow = WorkflowDefinition(
                name="test_flow",
                steps=[NeedsGuidanceStep(), FinishStep()],
                continue_on_failure=False,
            )
            engine.register_workflow(workflow)

            paused_result = engine.execute("test_flow", {"project_path": "/tmp/project"})
            self.assertTrue(paused_result.get("needs_guidance"))
            self.assertIn("_workflow_id", paused_result)
            workflow_id = paused_result["_workflow_id"]

            self.assertTrue(Path(state_file).exists())
            persisted = Path(state_file).read_text(encoding="utf-8")
            self.assertIn(workflow_id, persisted)
            persistence_status = engine.get_persistence_status()
            self.assertTrue(persistence_status["enabled"])
            self.assertEqual(persistence_status["state_store_path"], state_file)
            self.assertIn(workflow_id, persistence_status["persisted_workflow_ids"])

            # Simulate restart by creating a new engine bound to same state file.
            new_engine = WorkflowEngine(EventEmitter(), state_store_path=state_file)
            new_engine.register_workflow(workflow)
            resumed = new_engine.resume(workflow_id, "approved-guidance")

            self.assertTrue(resumed.get("success"))
            self.assertEqual(resumed.get("guided_value"), "approved-guidance")
            self.assertTrue(resumed.get("done"))

            # Ensure completed workflow no longer stays active.
            self.assertEqual(len(new_engine.get_active_workflows()), 0)
            final_status = new_engine.get_persistence_status()
            self.assertEqual(final_status["persisted_workflow_ids"], [])


if __name__ == "__main__":
    unittest.main()
