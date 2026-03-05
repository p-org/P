"""MCP resources for P language guides."""


def register_resources(mcp, get_services):
    """Register MCP resources."""

    def _load_resource(path: str) -> str:
        services = get_services()
        try:
            return services["resources"].load(f"context_files/{path}")
        except Exception as e:
            return f"Error loading resource: {e}"

    @mcp.resource("p://guides/syntax")
    def get_syntax_guide() -> str:
        """Complete P language syntax reference"""
        return _load_resource("P_syntax_guide.txt")

    @mcp.resource("p://guides/basics")
    def get_basics_guide() -> str:
        """P language fundamentals and core concepts"""
        return _load_resource("modular/p_basics.txt")

    @mcp.resource("p://guides/machines")
    def get_machines_guide() -> str:
        """Guide to P state machines"""
        return _load_resource("modular/p_machines_guide.txt")

    @mcp.resource("p://guides/types")
    def get_types_guide() -> str:
        """P language type system guide"""
        return _load_resource("modular/p_types_guide.txt")

    @mcp.resource("p://guides/events")
    def get_events_guide() -> str:
        """P language events guide"""
        return _load_resource("modular/p_events_guide.txt")

    @mcp.resource("p://guides/enums")
    def get_enums_guide() -> str:
        """P language enums guide"""
        return _load_resource("modular/p_enums_guide.txt")

    @mcp.resource("p://guides/statements")
    def get_statements_guide() -> str:
        """P language statements guide"""
        return _load_resource("modular/p_statements_guide.txt")

    @mcp.resource("p://guides/specs")
    def get_specs_guide() -> str:
        """P specification and monitors guide"""
        return _load_resource("modular/p_spec_monitors_guide.txt")

    @mcp.resource("p://guides/tests")
    def get_tests_guide() -> str:
        """P test cases guide"""
        return _load_resource("modular/p_test_cases_guide.txt")

    @mcp.resource("p://guides/modules")
    def get_modules_guide() -> str:
        """P module system guide"""
        return _load_resource("modular/p_module_system_guide.txt")

    @mcp.resource("p://guides/compiler")
    def get_compiler_guide() -> str:
        """P compiler usage and error guide"""
        return _load_resource("modular/p_compiler_guide.txt")

    @mcp.resource("p://guides/common_errors")
    def get_common_errors_guide() -> str:
        """Common P compilation errors and fixes"""
        return _load_resource("modular/p_common_compilation_errors.txt")

    @mcp.resource("p://examples/program")
    def get_program_example() -> str:
        """Complete P program example"""
        return _load_resource("modular/p_program_example.txt")

    @mcp.resource("p://about")
    def get_about_p() -> str:
        """About the P language"""
        return _load_resource("about_p.txt")

    return {
        "get_syntax_guide": get_syntax_guide,
        "get_basics_guide": get_basics_guide,
        "get_machines_guide": get_machines_guide,
        "get_types_guide": get_types_guide,
        "get_events_guide": get_events_guide,
        "get_enums_guide": get_enums_guide,
        "get_statements_guide": get_statements_guide,
        "get_specs_guide": get_specs_guide,
        "get_tests_guide": get_tests_guide,
        "get_modules_guide": get_modules_guide,
        "get_compiler_guide": get_compiler_guide,
        "get_common_errors_guide": get_common_errors_guide,
        "get_program_example": get_program_example,
        "get_about_p": get_about_p,
    }
