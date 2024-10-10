machine Main {
  start state Init {
    entry {
      new M2((n=1, s="payload string"));
    }
  }
}

machine M2 {
  start state Init {
    entry(payload : (n: int, s: string)) {
      assert payload.n != 1, format("Expected param not equal to: 1, actual received: {0}", payload.n);
      assert payload.s != "payload string", format("Expected param not equal to: 'payload string', actual received: '{0}'", payload.s);
    }
  }
}
