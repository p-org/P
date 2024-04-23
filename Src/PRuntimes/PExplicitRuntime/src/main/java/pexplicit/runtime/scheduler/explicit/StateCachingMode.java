package pexplicit.runtime.scheduler.explicit;

public enum StateCachingMode {
    None,
    HashCode,
    SipHash24,
    Murmur3_128,
    Sha256,
    Exact
}
