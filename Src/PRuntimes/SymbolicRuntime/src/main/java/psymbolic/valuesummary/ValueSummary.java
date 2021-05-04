package psymbolic.valuesummary;


public interface ValueSummary<T extends ValueSummary> {

    /**
     * Casts an AnyVS (UnionVS<TypeTag>) to a ValueSummary type. If there is some non
     * constantly false path constraint under which the current pc is defined but not the guard
     * corresponding to the specified type, the function throws a ClassCastException.
     * If the ValueSummary type is also a UnionVS, returns the provided UnionVS.
     *
     * @param pc The path constraint guard to cast under
     * @param type The ValueSummary type to cast to
     * @param src The UnionVS to cast from
     * @return A ValueSummary that can be casted into the provided type
     */
     static ValueSummary fromAny(Guard cast_pc_guard, Class<? extends ValueSummary> type, UnionVS anyVal) {
         ValueSummary result;
         if (type.equals(UnionVS.class)) {
             result = anyVal;
         } else {
             Guard typeGuard = ((PrimitiveVS<?>)anyVal.getType()).getGuard(type);
             Guard pcNotDefined = cast_pc_guard.and(typeGuard.not());
             if (!pcNotDefined.isFalse()) {
                 throw new ClassCastException(String.format("Casting to %s under path constraint %s is not defined",
                         type,
                         pcNotDefined));
             }
             result = anyVal.getPayload(type).guard(cast_pc_guard);
         }
         return result;
     }

    /** Check whether a value summary has any values under any path condition
     *
     * @return Whether the path condition is empty
     */
    public boolean isEmptyVS();

    /** Restrict the value summary's universe with a provided guard
     *
     * @param guard The guard to conjoin to the current value summary's universe
     * @return The result of restricting the value summary's universe
     */
    public T guard(Guard guard);

    /** Merge the value summary with other provided value summaries
     *
     * @param summaries The summaries to merge the value summary with
     * @return The result of the merging
     */
    public T merge(Iterable<T> summaries);

    /** Merge the value summary with another value summary
     *
     * @param summary The summary to merge the value summary with
     * @return The result of the merging
     */
    public T merge(T summary);

    /** Create a new value summary that is equal to the `update` value when the `guard` is true
     * and same as the old value when the `guard` is not true
     *
     * @param guard The condition under which the value summary should be updated
     * @param update The value to update the value summary to
     * @return The result of the update
     */
    public T update(Guard guard, T update);

    /** Check whether the value summary is equal to another value summary
     *
     * @param cmp The summary to compare the value summary with
     * @param guard The guard for the universe of the result
     * @return Whether or not the value summaries are equal
     */
    PrimitiveVS<Boolean> symbolicEquals(T cmp, Guard guard);

    /** Get the Guard that represents the universe of the value summary
     *
     * @return The value summary's universe
     */
    Guard getUniverse();
}
