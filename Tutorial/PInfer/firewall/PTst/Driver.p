fun SetupSystem(num_internal: int, num_external: int) {
    var internalNodes: set[machine];
    var externalNodes: set[machine];
    var firewall: Firewall;
    var i: int;
    var m: machine;

    internalNodes = default(set[machine]);
    externalNodes = default(set[machine]);
    i = 0;
    firewall = new Firewall();
    while (i < num_external) {
        externalNodes += (new ExternalServer((firewall=firewall,)));
        i = i + 1;
    }
    i = 0;
    while (i < num_internal) {
        internalNodes += (new InternalServer((external=externalNodes, firewall=firewall)));
        i = i + 1;
    }
    i = 0;
    foreach (m in externalNodes) {
        send m, Internal, (nodes=internalNodes,);
    }
}

machine I3E1 {
    start state Init {
        entry {
            SetupSystem(3, 1);
        }
    }
}

machine I1E3 {
    start state Init {
        entry {
            SetupSystem(1, 3);
        }
    }
}

machine I3E3 {
    start state Init {
        entry {
            SetupSystem(3, 3);
        }
    }
}

machine I4E5 {
    start state Init {
        entry {
            SetupSystem(4, 5);
        }
    }
}

test tcI3E1 [main=I3E1]:
    assert Safety in (union { I3E1 }, FirewallMod);

test tcI1E3 [main=I1E3]:
    assert Safety in (union { I1E3 }, FirewallMod);

test tcI3E3 [main=I3E3]:
    assert Safety in (union { I3E3 }, FirewallMod);

test tcI4E5 [main=I4E5]:
    assert Safety in (union { I4E5 }, FirewallMod);