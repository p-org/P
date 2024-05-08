package pexplicit.runtime.scheduler.explicit.strategy;

public enum SearchStrategyMode {
    DepthFirst("dfs"),
    Random("random"),
    Astar("astar");

    private String name;

    /**
     * Constructor
     *
     * @param n Name of the enum
     */
    SearchStrategyMode(String n) {
        this.name = n;
    }

    @Override
    public String toString() {
        return this.name;
    }
}
