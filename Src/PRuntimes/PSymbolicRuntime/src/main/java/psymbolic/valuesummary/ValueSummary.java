package psymbolic.valuesummary;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.List;

public interface ValueSummary<T extends ValueSummary<T>> extends Serializable {

    static UnionVS castToAny(Guard pc, ValueSummary<?> toCast) {
        if (toCast instanceof UnionVS) { return (UnionVS) toCast.restrict(pc); }
        return new UnionVS(toCast).restrict(pc);
    }

    /**
     * Casts an AnyVS to a ValueSummary type. If there is some non
     * constantly false path constraint under which the current pc is defined but not the guard
     * corresponding to the specified type, the function throws a ClassCastException.
     * If the ValueSummary type is also a UnionVS, returns the provided UnionVS.
     *
     * @param pc The path constraint guard to cast under
     * @param def The default value of the ValueSummary type to cast to
     * @param anyVal The UnionVS to cast from
     * @return A ValueSummary that can be casted into the provided type
     */
     static ValueSummary<?> castFromAny(Guard pc, ValueSummary<?> def, UnionVS anyVal) {
         ValueSummary<?> result;
         if (def instanceof UnionVS) {
             return anyVal;
         }
         Class<? extends ValueSummary> type = def.getClass();
         Guard typeGuard = anyVal.getGuardFor(type);
         Guard pcNotDefined = pc.and(typeGuard.not());
         Guard pcDefined = pc.and(typeGuard);
         if (pcDefined.isFalse()) {
             if (type.equals(PrimitiveVS.class)) {
                 return new PrimitiveVS<>(pc);
             }
             System.out.println(anyVal.restrict(typeGuard));
             throw new ClassCastException(String.format("Casting to %s under path constraint %s is not defined",
                     type,
                     pcNotDefined));
         }
         result = anyVal.getValue(type).restrict(pc);
/*
         if (!pcNotDefined.isFalse()) {
             if (type.equals(PrimitiveVS.class)) {
                 return new PrimitiveVS<>(pc);
             }
             System.out.println(anyVal.restrict(typeGuard));
             throw new ClassCastException(String.format("Casting to %s under path constraint %s is not defined",
                     type,
                     pcNotDefined));
         }
         result = anyVal.getValue(type).restrict(pc);
*/
         /*
         if (type.equals(NamedTupleVS.class)) {
             NamedTupleVS namedTupleDefault = (NamedTupleVS) def.restrict(pc);
             NamedTupleVS namedTupleResult = (NamedTupleVS) result;
             namedTupleResult.getNames();
             String[] defaultNames = namedTupleDefault.getNames();
             String[] resultNames = namedTupleResult.getNames();
             for (int i = 0; i < defaultNames.length; i++) {
                 String name = defaultNames[i];
                 if (!resultNames[i].equals(defaultNames[i])) {
                     throw new ClassCastException(
                             String.format("Casting to %s under path constraint %s is not defined." +
                                             "Named tuple field names do not match.",
                             type,
                             pcNotDefined));
                 }
                 ValueSummary<?> defaultField = namedTupleResult.getField(name);
                 ValueSummary<?> resultField = namedTupleResult.getField(name);
                 Class<?> defaultFieldType = defaultField.getClass();
                 System.out.println("Field " + name + " has default type " + defaultFieldType.getCanonicalName());
                 if (!resultField.getClass().equals(defaultFieldType)) {
                     if (resultField instanceof UnionVS) {
                         System.out.println("need to cast nested");
                         namedTupleResult = namedTupleResult.setField(name,
                                 castFromAny(pc, defaultField, (UnionVS) resultField));
                     } else {
                         throw new ClassCastException(
                                 String.format("Casting to %s under path constraint %s is not defined." +
                                                 " Named tuple field types do not match.",
                                         type,
                                         pcNotDefined));
                     }
                 }
             }
             result = namedTupleResult;
         }
          */
         return result;
     }

    /**
     * Check whether a value summary has any values under any path condition
     *
     * @return Whether the path condition is empty
     */
    boolean isEmptyVS();

    /**
     * Restrict the value summary's universe with a provided guard
     *
     * @param guard The guard to conjoin to the current value summary's universe
     * @return The result of restricting the value summary's universe
     */
    T restrict(Guard guard);

    /**
     * Merge the value summary with other provided value summaries
     *
     * @param summaries The summaries to merge the value summary with
     * @return The result of the merging
     */
    T merge(Iterable<T> summaries);

    /**
     * Merge the value summary with another value summary
     *
     * @param summary The summary to merge the value summary with
     * @return The result of the merging
     */
    T merge(T summary);

    /**
     * Create a new value summary that is equal to the `update` value under the given `guard`
     * and same as the old value otherwise `!guard`
     *
     * @param guard The condition under which the value summary should be updated
     * @param updateVal The value to update the value summary to
     * @return The result of the update
     */
    T updateUnderGuard(Guard guard, T updateVal);

    /**
     * Check whether the value summary is equal to another value summary
     *
     * @param cmp The other value summary
     * @param guard The guard for the universe of the equality result
     * @return Boolean VS representing the guard under which the two value summaries are equal
     */
    PrimitiveVS<Boolean> symbolicEquals(T cmp, Guard guard);

    /**
     * Get the Guard that represents the universe of the value summary
     * Disjunction of the guards of all the guarded values
     * @return The universe of the value summary
     */
    Guard getUniverse();

    /**
     * Copy the value summary
     *
     * @return A new cloned copy of the value summary
     */
    T getCopy();
}
