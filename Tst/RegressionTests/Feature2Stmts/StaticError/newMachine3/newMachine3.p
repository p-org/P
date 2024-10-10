
machine Main {
  start state Init {
    entry {
      new M2((n="payload string", s=1));
    }
  }
}

machine M2 {
  start state Init {
    entry(payload : (n: int, s: string)) {
    }
  }
}
