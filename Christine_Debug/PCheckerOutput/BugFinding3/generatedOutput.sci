<CoverageInfo z:Id="1" xmlns="http://schemas.datacontract.org/2004/07/PChecker.Coverage" xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns:z="http://schemas.microsoft.com/2003/10/Serialization/"><CoverageGraph z:Id="2"><InternalAllocatedLinkIds z:Id="3" z:Size="4" xmlns:a="http://schemas.microsoft.com/2003/10/Serialization/Arrays"><a:KeyValueOfstringstring><a:Key z:Id="4">PImplementation.CMachine.Init-&gt;PImplementation.B.StateMachine(0)</a:Key><a:Value z:Id="5">E</a:Value></a:KeyValueOfstringstring><a:KeyValueOfstringstring><a:Key z:Id="6">PImplementation.CMachine.Init-&gt;PImplementation.CMonitor.Init(0)</a:Key><a:Value z:Id="7">G</a:Value></a:KeyValueOfstringstring><a:KeyValueOfstringstring><a:Key z:Id="8">PImplementation.CMonitor.Init-&gt;PImplementation.CMonitor.Init(0)</a:Key><a:Value z:Ref="7" i:nil="true"/></a:KeyValueOfstringstring><a:KeyValueOfstringstring><a:Key z:Id="9">PImplementation.CMachine.Init-&gt;PImplementation.B.StateMachine(1)</a:Key><a:Value z:Id="10">G</a:Value></a:KeyValueOfstringstring></InternalAllocatedLinkIds><InternalAllocatedLinkIndexes z:Id="11" z:Size="4" xmlns:a="http://schemas.microsoft.com/2003/10/Serialization/Arrays"><a:KeyValueOfstringint><a:Key z:Id="12">PImplementation.CMachine.Init-&gt;PImplementation.B.StateMachine(E)</a:Key><a:Value>0</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key z:Id="13">PImplementation.CMachine.Init-&gt;PImplementation.CMonitor.Init(G)</a:Key><a:Value>0</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key z:Id="14">PImplementation.CMonitor.Init-&gt;PImplementation.CMonitor.Init(G)</a:Key><a:Value>0</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key z:Id="15">PImplementation.CMachine.Init-&gt;PImplementation.B.StateMachine(G)</a:Key><a:Value>1</a:Value></a:KeyValueOfstringint></InternalAllocatedLinkIndexes><InternalLinks z:Id="16" z:Size="8" xmlns:a="http://schemas.microsoft.com/2003/10/Serialization/Arrays"><a:KeyValueOfstringGraphLink5QResVQF><a:Key z:Id="17">PImplementation.CMachine-&gt;PImplementation.CMachine.Init</a:Key><a:Value z:Id="18"><AttributeLists i:nil="true"/><Attributes i:nil="true"/><Category z:Id="19">Contains</Category><Index i:nil="true"/><Label i:nil="true"/><Source z:Id="20"><AttributeLists i:nil="true"/><Attributes z:Id="21" z:Size="1"><a:KeyValueOfstringstring><a:Key z:Id="22">Group</a:Key><a:Value z:Id="23">Expanded</a:Value></a:KeyValueOfstringstring></Attributes><Category z:Id="24">StateMachine</Category><Id z:Id="25">PImplementation.CMachine</Id><Label i:nil="true"/></Source><Target z:Id="26"><AttributeLists i:nil="true"/><Attributes i:nil="true"/><Category i:nil="true"/><Id z:Id="27">PImplementation.CMachine.Init</Id><Label z:Id="28">Init</Label></Target></a:Value></a:KeyValueOfstringGraphLink5QResVQF><a:KeyValueOfstringGraphLink5QResVQF><a:Key z:Id="29">PImplementation.B-&gt;PImplementation.B.Init</a:Key><a:Value z:Id="30"><AttributeLists i:nil="true"/><Attributes i:nil="true"/><Category z:Ref="19" i:nil="true"/><Index i:nil="true"/><Label i:nil="true"/><Source z:Id="31"><AttributeLists i:nil="true"/><Attributes z:Id="32" z:Size="1"><a:KeyValueOfstringstring><a:Key z:Ref="22" i:nil="true"/><a:Value z:Ref="23" i:nil="true"/></a:KeyValueOfstringstring></Attributes><Category z:Ref="24" i:nil="true"/><Id z:Id="33">PImplementation.B</Id><Label i:nil="true"/></Source><Target z:Id="34"><AttributeLists i:nil="true"/><Attributes i:nil="true"/><Category i:nil="true"/><Id z:Id="35">PImplementation.B.Init</Id><Label z:Id="36">Init</Label></Target></a:Value></a:KeyValueOfstringGraphLink5QResVQF><a:KeyValueOfstringGraphLink5QResVQF><a:Key z:Id="37">PImplementation.B-&gt;PImplementation.B.StateMachine</a:Key><a:Value z:Id="38"><AttributeLists i:nil="true"/><Attributes i:nil="true"/><Category z:Ref="19" i:nil="true"/><Index i:nil="true"/><Label i:nil="true"/><Source z:Ref="31" i:nil="true"/><Target z:Id="39"><AttributeLists i:nil="true"/><Attributes i:nil="true"/><Category i:nil="true"/><Id z:Id="40">PImplementation.B.StateMachine</Id><Label z:Ref="36" i:nil="true"/></Target></a:Value></a:KeyValueOfstringGraphLink5QResVQF><a:KeyValueOfstringGraphLink5QResVQF><a:Key z:Id="41">PImplementation.CMachine.Init-&gt;PImplementation.B.StateMachine(0)</a:Key><a:Value z:Id="42"><AttributeLists i:nil="true"/><Attributes z:Id="43" z:Size="1"><a:KeyValueOfstringstring><a:Key z:Id="44">EventId</a:Key><a:Value z:Id="45">PImplementation.E</a:Value></a:KeyValueOfstringstring></Attributes><Category i:nil="true"/><Index>0</Index><Label z:Ref="5" i:nil="true"/><Source z:Ref="26" i:nil="true"/><Target z:Ref="39" i:nil="true"/></a:Value></a:KeyValueOfstringGraphLink5QResVQF><a:KeyValueOfstringGraphLink5QResVQF><a:Key z:Id="46">PImplementation.CMonitor-&gt;PImplementation.CMonitor.Init</a:Key><a:Value z:Id="47"><AttributeLists i:nil="true"/><Attributes i:nil="true"/><Category z:Ref="19" i:nil="true"/><Index i:nil="true"/><Label i:nil="true"/><Source z:Id="48"><AttributeLists i:nil="true"/><Attributes z:Id="49" z:Size="1"><a:KeyValueOfstringstring><a:Key z:Ref="22" i:nil="true"/><a:Value z:Ref="23" i:nil="true"/></a:KeyValueOfstringstring></Attributes><Category z:Id="50">Monitor</Category><Id z:Id="51">PImplementation.CMonitor</Id><Label z:Ref="51" i:nil="true"/></Source><Target z:Id="52"><AttributeLists i:nil="true"/><Attributes i:nil="true"/><Category i:nil="true"/><Id z:Id="53">PImplementation.CMonitor.Init</Id><Label z:Id="54">Init</Label></Target></a:Value></a:KeyValueOfstringGraphLink5QResVQF><a:KeyValueOfstringGraphLink5QResVQF><a:Key z:Id="55">PImplementation.CMachine.Init-&gt;PImplementation.CMonitor.Init(0)</a:Key><a:Value z:Id="56"><AttributeLists i:nil="true"/><Attributes z:Id="57" z:Size="1"><a:KeyValueOfstringstring><a:Key z:Ref="44" i:nil="true"/><a:Value z:Ref="7" i:nil="true"/></a:KeyValueOfstringstring></Attributes><Category i:nil="true"/><Index>0</Index><Label z:Ref="7" i:nil="true"/><Source z:Ref="26" i:nil="true"/><Target z:Ref="52" i:nil="true"/></a:Value></a:KeyValueOfstringGraphLink5QResVQF><a:KeyValueOfstringGraphLink5QResVQF><a:Key z:Id="58">PImplementation.CMonitor.Init-&gt;PImplementation.CMonitor.Init(0)</a:Key><a:Value z:Id="59"><AttributeLists i:nil="true"/><Attributes z:Id="60" z:Size="1"><a:KeyValueOfstringstring><a:Key z:Ref="44" i:nil="true"/><a:Value z:Ref="7" i:nil="true"/></a:KeyValueOfstringstring></Attributes><Category i:nil="true"/><Index>0</Index><Label z:Ref="7" i:nil="true"/><Source z:Ref="52" i:nil="true"/><Target z:Ref="52" i:nil="true"/></a:Value></a:KeyValueOfstringGraphLink5QResVQF><a:KeyValueOfstringGraphLink5QResVQF><a:Key z:Id="61">PImplementation.CMachine.Init-&gt;PImplementation.B.StateMachine(1)</a:Key><a:Value z:Id="62"><AttributeLists i:nil="true"/><Attributes z:Id="63" z:Size="1"><a:KeyValueOfstringstring><a:Key z:Ref="44" i:nil="true"/><a:Value z:Id="64">PImplementation.G</a:Value></a:KeyValueOfstringstring></Attributes><Category i:nil="true"/><Index>1</Index><Label z:Ref="10" i:nil="true"/><Source z:Ref="26" i:nil="true"/><Target z:Ref="39" i:nil="true"/></a:Value></a:KeyValueOfstringGraphLink5QResVQF></InternalLinks><InternalNextLinkIndex z:Id="65" z:Size="3" xmlns:a="http://schemas.microsoft.com/2003/10/Serialization/Arrays"><a:KeyValueOfstringint><a:Key z:Id="66">PImplementation.CMachine.Init-&gt;PImplementation.B.StateMachine</a:Key><a:Value>1</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key z:Id="67">PImplementation.CMachine.Init-&gt;PImplementation.CMonitor.Init</a:Key><a:Value>0</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key z:Id="68">PImplementation.CMonitor.Init-&gt;PImplementation.CMonitor.Init</a:Key><a:Value>0</a:Value></a:KeyValueOfstringint></InternalNextLinkIndex><InternalNodes z:Id="69" z:Size="7" xmlns:a="http://schemas.microsoft.com/2003/10/Serialization/Arrays"><a:KeyValueOfstringGraphNode5QResVQF><a:Key z:Ref="51" i:nil="true"/><a:Value z:Ref="48" i:nil="true"/></a:KeyValueOfstringGraphNode5QResVQF><a:KeyValueOfstringGraphNode5QResVQF><a:Key z:Ref="25" i:nil="true"/><a:Value z:Ref="20" i:nil="true"/></a:KeyValueOfstringGraphNode5QResVQF><a:KeyValueOfstringGraphNode5QResVQF><a:Key z:Ref="27" i:nil="true"/><a:Value z:Ref="26" i:nil="true"/></a:KeyValueOfstringGraphNode5QResVQF><a:KeyValueOfstringGraphNode5QResVQF><a:Key z:Ref="33" i:nil="true"/><a:Value z:Ref="31" i:nil="true"/></a:KeyValueOfstringGraphNode5QResVQF><a:KeyValueOfstringGraphNode5QResVQF><a:Key z:Ref="35" i:nil="true"/><a:Value z:Ref="34" i:nil="true"/></a:KeyValueOfstringGraphNode5QResVQF><a:KeyValueOfstringGraphNode5QResVQF><a:Key z:Ref="40" i:nil="true"/><a:Value z:Ref="39" i:nil="true"/></a:KeyValueOfstringGraphNode5QResVQF><a:KeyValueOfstringGraphNode5QResVQF><a:Key z:Ref="53" i:nil="true"/><a:Value z:Ref="52" i:nil="true"/></a:KeyValueOfstringGraphNode5QResVQF></InternalNodes></CoverageGraph><EventInfo z:Id="70"><EventsReceived z:Id="71" z:Size="2" xmlns:a="http://schemas.microsoft.com/2003/10/Serialization/Arrays"><a:KeyValueOfstringArrayOfstringty7Ep6D1><a:Key z:Id="72">PImplementation.B.Init</a:Key><a:Value z:Id="73" z:Size="2"><a:string z:Ref="45" i:nil="true"/><a:string z:Ref="64" i:nil="true"/></a:Value></a:KeyValueOfstringArrayOfstringty7Ep6D1><a:KeyValueOfstringArrayOfstringty7Ep6D1><a:Key z:Id="74">PImplementation.CMonitor.Init</a:Key><a:Value z:Id="75" z:Size="1"><a:string z:Ref="64" i:nil="true"/></a:Value></a:KeyValueOfstringArrayOfstringty7Ep6D1></EventsReceived><EventsSent z:Id="76" z:Size="1" xmlns:a="http://schemas.microsoft.com/2003/10/Serialization/Arrays"><a:KeyValueOfstringArrayOfstringty7Ep6D1><a:Key z:Id="77">PImplementation.CMachine.Init</a:Key><a:Value z:Id="78" z:Size="2"><a:string z:Ref="45" i:nil="true"/><a:string z:Ref="64" i:nil="true"/></a:Value></a:KeyValueOfstringArrayOfstringty7Ep6D1></EventsSent></EventInfo><Machines z:Id="79" z:Size="3" xmlns:a="http://schemas.microsoft.com/2003/10/Serialization/Arrays"><a:string z:Ref="51" i:nil="true"/><a:string z:Ref="25" i:nil="true"/><a:string z:Ref="33" i:nil="true"/></Machines><MachinesToStates z:Id="80" z:Size="3" xmlns:a="http://schemas.microsoft.com/2003/10/Serialization/Arrays"><a:KeyValueOfstringArrayOfstringty7Ep6D1><a:Key z:Ref="51" i:nil="true"/><a:Value z:Id="81" z:Size="1"><a:string z:Ref="54" i:nil="true"/></a:Value></a:KeyValueOfstringArrayOfstringty7Ep6D1><a:KeyValueOfstringArrayOfstringty7Ep6D1><a:Key z:Ref="25" i:nil="true"/><a:Value z:Id="82" z:Size="1"><a:string z:Ref="28" i:nil="true"/></a:Value></a:KeyValueOfstringArrayOfstringty7Ep6D1><a:KeyValueOfstringArrayOfstringty7Ep6D1><a:Key z:Ref="33" i:nil="true"/><a:Value z:Id="83" z:Size="1"><a:string z:Ref="36" i:nil="true"/></a:Value></a:KeyValueOfstringArrayOfstringty7Ep6D1></MachinesToStates><RegisteredEvents z:Id="84" z:Size="1" xmlns:a="http://schemas.microsoft.com/2003/10/Serialization/Arrays"><a:KeyValueOfstringArrayOfstringty7Ep6D1><a:Key z:Id="85">PImplementation.CMonitor.Init</a:Key><a:Value z:Id="86" z:Size="1"><a:string z:Ref="64" i:nil="true"/></a:Value></a:KeyValueOfstringArrayOfstringty7Ep6D1></RegisteredEvents></CoverageInfo>