event SendFromInternal: (src: machine, dst: machine);
event SendToInternal: (src: machine, dst: machine);
event Allowed: (node: machine);
event Sent: (src: machine, dst: machine, toInternal: bool);
event Internal: (nodes: set[machine]);

machine Firewall {
    var allowed: set[machine];

    start state Serving {
        on SendFromInternal do (pld: (src: machine, dst: machine)) {
            announce Allowed, (node=pld.dst,);
            allowed += (pld.dst);
            announce Sent, (src=pld.src, dst=pld.dst, toInternal=false);
        }

        on SendToInternal do (pld: (src: machine, dst: machine)) {
            if (pld.src in allowed) {
                announce Sent, (src=pld.src, dst=pld.dst, toInternal=true);
            }
        }
    }
}

machine InternalServer {
    var externalNodes: set[machine];
    var firewall: Firewall;

    start state Init {
        entry (pld: (external: set[machine], firewall: Firewall)) {
            externalNodes = pld.external;
            firewall = pld.firewall;
            goto Serving;
        }
    }

    state Serving {
        entry {
            var dst: machine;
            dst = choose(externalNodes);
            send firewall, SendFromInternal, (src=this, dst=dst);
        }
    }
}

machine ExternalServer {
    var internalNodes: set[machine];
    var firewall: Firewall;

    start state Init {
        entry (pld: (firewall: Firewall)) {
            firewall = pld.firewall;
        }

        on Internal do (pld: (nodes: set[machine])) {
            internalNodes = pld.nodes;
            goto Serving;
        }
    }

    state Serving {
        entry {
            var dst: machine;
            dst = choose(internalNodes);
            send firewall, SendToInternal, (src=this, dst=dst);
        }
    }
}

spec Safety observes Sent, Sent, Allowed {
    var allowed: set[machine];

    start state Init {
        entry {
            allowed = default(set[machine]);
            goto Listening;
        }
    }

    state Listening {
        on Sent do (pld: (src: machine, dst: machine, toInternal: bool)) {
            if (pld.toInternal) {
                assert pld.src in allowed;
            } else {
                allowed += (pld.dst);
            }
        }

        on Allowed do (pld: (node: machine)) {
            allowed += (pld.node);
        }
    }
}

module FirewallMod = {Firewall, InternalServer, ExternalServer};