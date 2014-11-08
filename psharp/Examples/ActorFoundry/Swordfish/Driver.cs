using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    [Main]
    internal class Driver : Machine
    {
        private Machine Bank;
        private Machine ATM;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] begins testing ...\n");

                machine.Bank = Machine.Factory.CreateMachine<Bank>(machine);
                machine.ATM = Machine.Factory.CreateMachine<ATM>(machine.Bank);

                this.Raise(new eLocal());
            }
        }

        private class Test1 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 10000, "1234", 1, 1.0)));
            }
        }

        private class Test2 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Close account testing ...\n");

                this.Send(machine.Bank, new eCloseAccount(new Transaction(
                    machine, typeof(eCreateCallback), 1, 0, "1234", 1, 1.0)));
            }
        }

        private class Test3 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Close account testing ...\n");

                this.Send(machine.Bank, new eCloseAccount(new Transaction(
                    machine, typeof(eCreateCallback), 2, 0, "1234", 1, 1.0)));
            }
        }

        private class Test4 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 0, "1234", 0, 1.0)));
            }
        }

        private class Test5 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Close account testing ...\n");

                this.Send(machine.Bank, new eCloseAccount(new Transaction(
                    machine, typeof(eCreateCallback), 2, 0, "1234", 1, 1.0)));
            }
        }

        private class Test6 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Close account testing ...\n");

                this.Send(machine.Bank, new eCloseAccount(new Transaction(
                    machine, typeof(eCreateCallback), 2, 0, "1234", 1, 1.0)));
            }
        }

        private class Test7 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 0, "1234", 1, 1.0)));
            }
        }

        private class Test8 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Deposit account testing ...\n");

                this.Send(machine.Bank, new eDeposit(new Transaction(
                    machine, typeof(eCreateCallback), 3, 2500, "1234", 1, 1.0)));
            }
        }

        private class Test9 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Deposit account testing ...\n");

                this.Send(machine.Bank, new eDeposit(new Transaction(
                    machine, typeof(eCreateCallback), 15, 1000, "1234", 1, 1.0)));
            }
        }

        private class Test10 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Withdraw account testing ...\n");

                this.Send(machine.Bank, new eWithdraw(new Transaction(
                    machine, typeof(eCreateCallback), 3, 2000, "1234", 1, 1.0)));
            }
        }

        private class Test11 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Withdraw account testing ...\n");

                this.Send(machine.Bank, new eWithdraw(new Transaction(
                    machine, typeof(eCreateCallback), 15, 500, "1234", 1, 1.0)));
            }
        }

        private class Test12 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 4000, "1234", 1, 1.0)));
            }
        }

        private class Test13 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Transfer account testing ...\n");

                this.Send(machine.Bank, new eTransfer(new Transfer(
                    machine, typeof(eCreateCallback), 4, 3, 2000, "1234")));
            }
        }

        private class Test14 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Transfer account testing ...\n");

                this.Send(machine.Bank, new eTransfer(new Transfer(
                    machine, typeof(eCreateCallback), 20, 3, 200, "1234")));
            }
        }

        private class Test15 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Transfer account testing ...\n");

                this.Send(machine.Bank, new eTransfer(new Transfer(
                    machine, typeof(eCreateCallback), 4, 30, 200, "1234")));
            }
        }

        private class Test16 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 2500, "1234", 1, 1.0)));
            }
        }

        private class Test17 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.Bank, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 5, 0, "1234", 1, 1.0)));
            }
        }

        private class Test18 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.Bank, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 3, 0, "1234", 1, 1.0)));
            }
        }

        private class Test19 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.Bank, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 10, 0, "1234", 1, 1.0)));
            }
        }

        private class Test20 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Withdraw account testing ...\n");

                this.Send(machine.Bank, new eWithdraw(new Transaction(
                    machine, typeof(eCreateCallback), 5, 6000, "1234", 1, 1.0)));
            }
        }

        private class Test21 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 0, "1234", 1, 1.0)));
            }
        }

        private class Test22 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Deposit testing ...\n");

                this.Send(machine.Bank, new eDeposit(new Transaction(
                    machine, typeof(eCreateCallback), 6, 2500, "1234", 1, 1.0)));
            }
        }

        private class Test23 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Deposit testing ...\n");

                this.Send(machine.Bank, new eDeposit(new Transaction(
                    machine, typeof(eCreateCallback), 6, 1000, "1234", 1, 1.0)));
            }
        }

        private class Test24 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.Bank, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 6, 0, "1234", 1, 1.0)));
            }
        }

        private class Test25 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 0, "1234", 1, 1.0)));
            }
        }

        private class Test26 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Deposit testing ...\n");

                this.Send(machine.Bank, new eDeposit(new Transaction(
                    machine, typeof(eCreateCallback), 7, 3000, "1234", 1, 1.0)));
            }
        }

        private class Test27 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Withdraw account testing ...\n");

                this.Send(machine.Bank, new eWithdraw(new Transaction(
                    machine, typeof(eCreateCallback), 7, 1500, "1234", 1, 1.0)));
            }
        }

        private class Test28 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Withdraw account testing ...\n");

                this.Send(machine.Bank, new eWithdraw(new Transaction(
                    machine, typeof(eCreateCallback), 7, 2000, "1234", 1, 1.0)));
            }
        }

        private class Test29 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 4000, "1234", 1, 1.0)));
            }
        }

        private class Test30 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 4000, "1234", 1, 1.0)));
            }
        }

        private class Test31 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Transfer account testing ...\n");

                this.Send(machine.Bank, new eTransfer(new Transfer(
                    machine, typeof(eCreateCallback), 8, 9, 6000, "1234")));
            }
        }

        private class Test32 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Transfer account testing ...\n");

                this.Send(machine.Bank, new eTransfer(new Transfer(
                    machine, typeof(eCreateCallback), 8, 9, 2000, "1234")));
            }
        }

        private class Test33 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.Bank, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 8, 0, "1234", 1, 1.0)));
            }
        }

        private class Test34 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.Bank, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 9, 0, "1234", 1, 1.0)));
            }
        }

        private class Test35 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Lock account testing ...\n");

                this.Send(machine.Bank, new eLock(new Transaction(
                    machine, typeof(eCreateCallback), 12, 10000, "1234", 1, 1.0)));
            }
        }

        private class Test36 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Unlock account testing ...\n");

                this.Send(machine.Bank, new eUnlock(new Transaction(
                    machine, typeof(eCreateCallback), 12, 10000, "1234", 1, 1.0)));
            }
        }

        private class Test37 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 10000, "1234", 1, 1.0)));
            }
        }

        private class Test38 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Lock account testing ...\n");

                this.Send(machine.Bank, new eLock(new Transaction(
                    machine, typeof(eCreateCallback), 10, 10000, "1234", 1, 1.0)));
            }
        }

        private class Test39 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.Bank, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 10, 0, "1234", 1, 1.0)));
            }
        }

        private class Test40 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Unlock account testing ...\n");

                this.Send(machine.Bank, new eUnlock(new Transaction(
                    machine, typeof(eCreateCallback), 12, 10000, "1234", 1, 1.0)));
            }
        }

        private class Test41 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.Bank, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 10, 0, "1234", 1, 1.0)));
            }
        }

        private class Test42 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 10000, "1234", 1, 1.0)));
            }
        }

        private class Test43 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.Bank, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 11, 0, "1235", 1, 1.0)));
            }
        }

        private class Test44 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.Bank, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 11, 0, "1235", 1, 1.0)));
            }
        }

        private class Test45 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.Bank, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 11, 0, "1235", 1, 1.0)));
            }
        }

        private class Test46 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.Bank, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 11, 0, "1234", 1, 1.0)));
            }
        }

        private class Test47 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 0, "1234", 1, 1.0)));
            }
        }

        private class Test48 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Deposit testing ...\n");

                this.Send(machine.ATM, new eDeposit(new Transaction(
                    machine, typeof(eCreateCallback), 12, 2500, "1234", 1, 1.0)));
            }
        }

        private class Test49 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Deposit testing ...\n");

                this.Send(machine.ATM, new eDeposit(new Transaction(
                    machine, typeof(eCreateCallback), 15, 1000, "1234", 1, 1.0)));
            }
        }

        private class Test50 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Withdraw account testing ...\n");

                this.Send(machine.ATM, new eWithdraw(new Transaction(
                    machine, typeof(eCreateCallback), 12, 2000, "1234", 1, 1.0)));
            }
        }

        private class Test51 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Withdraw account testing ...\n");

                this.Send(machine.ATM, new eWithdraw(new Transaction(
                    machine, typeof(eCreateCallback), 13, 500, "1234", 1, 1.0)));
            }
        }

        private class Test52 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 4000, "1234", 1, 1.0)));
            }
        }

        private class Test53 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Transfer account testing ...\n");

                this.Send(machine.ATM, new eTransfer(new Transfer(
                    machine, typeof(eCreateCallback), 13, 12, 2000, "1234")));
            }
        }

        private class Test54 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Transfer account testing ...\n");

                this.Send(machine.ATM, new eTransfer(new Transfer(
                    machine, typeof(eCreateCallback), 20, 12, 200, "1234")));
            }
        }

        private class Test55 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Transfer account testing ...\n");

                this.Send(machine.ATM, new eTransfer(new Transfer(
                    machine, typeof(eCreateCallback), 13, 30, 200, "1234")));
            }
        }

        private class Test56 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Create account testing ...\n");

                this.Send(machine.Bank, new eCreateAccount(new Transaction(
                    machine, typeof(eCreateCallback), null, 2500, "1234", 1, 1.0)));
            }
        }

        private class Test57 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.ATM, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 14, 0, "1234", 1, 1.0)));
            }
        }

        private class Test58 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.ATM, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 12, 0, "1234", 1, 1.0)));
            }
        }

        private class Test59 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Balance inquiry testing ...\n");

                this.Send(machine.ATM, new eBalanceInquiry(new Transaction(
                    machine, typeof(eCreateCallback), 20, 0, "1234", 1, 1.0)));
            }
        }

        private class End : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] is Ending ...\n");

                this.Send(machine.Bank, new eStop());

                this.Send(machine.ATM, new eStop());

                this.Delete();
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Test1));

            StepStateTransitions test1Dict = new StepStateTransitions();
            test1Dict.Add(typeof(eCreateCallback), typeof(Test2));

            StepStateTransitions test2Dict = new StepStateTransitions();
            test2Dict.Add(typeof(eCreateCallback), typeof(Test3));

            StepStateTransitions test3Dict = new StepStateTransitions();
            test3Dict.Add(typeof(eCreateCallback), typeof(Test4));

            StepStateTransitions test4Dict = new StepStateTransitions();
            test4Dict.Add(typeof(eCreateCallback), typeof(Test5));

            StepStateTransitions test5Dict = new StepStateTransitions();
            test5Dict.Add(typeof(eCreateCallback), typeof(Test6));

            StepStateTransitions test6Dict = new StepStateTransitions();
            test6Dict.Add(typeof(eCreateCallback), typeof(Test7));

            StepStateTransitions test7Dict = new StepStateTransitions();
            test7Dict.Add(typeof(eCreateCallback), typeof(Test8));

            StepStateTransitions test8Dict = new StepStateTransitions();
            test8Dict.Add(typeof(eCreateCallback), typeof(Test9));

            StepStateTransitions test9Dict = new StepStateTransitions();
            test9Dict.Add(typeof(eCreateCallback), typeof(Test10));

            StepStateTransitions test10Dict = new StepStateTransitions();
            test10Dict.Add(typeof(eCreateCallback), typeof(Test11));

            StepStateTransitions test11Dict = new StepStateTransitions();
            test11Dict.Add(typeof(eCreateCallback), typeof(Test12));

            StepStateTransitions test12Dict = new StepStateTransitions();
            test12Dict.Add(typeof(eCreateCallback), typeof(Test13));

            StepStateTransitions test13Dict = new StepStateTransitions();
            test13Dict.Add(typeof(eCreateCallback), typeof(Test14));

            StepStateTransitions test14Dict = new StepStateTransitions();
            test14Dict.Add(typeof(eCreateCallback), typeof(Test15));

            StepStateTransitions test15Dict = new StepStateTransitions();
            test15Dict.Add(typeof(eCreateCallback), typeof(Test16));

            StepStateTransitions test16Dict = new StepStateTransitions();
            test16Dict.Add(typeof(eCreateCallback), typeof(Test17));

            StepStateTransitions test17Dict = new StepStateTransitions();
            test17Dict.Add(typeof(eCreateCallback), typeof(Test18));

            StepStateTransitions test18Dict = new StepStateTransitions();
            test18Dict.Add(typeof(eCreateCallback), typeof(Test19));

            StepStateTransitions test19Dict = new StepStateTransitions();
            test19Dict.Add(typeof(eCreateCallback), typeof(Test20));

            StepStateTransitions test20Dict = new StepStateTransitions();
            test20Dict.Add(typeof(eCreateCallback), typeof(Test21));

            StepStateTransitions test21Dict = new StepStateTransitions();
            test21Dict.Add(typeof(eCreateCallback), typeof(Test22));

            StepStateTransitions test22Dict = new StepStateTransitions();
            test22Dict.Add(typeof(eCreateCallback), typeof(Test23));

            StepStateTransitions test23Dict = new StepStateTransitions();
            test23Dict.Add(typeof(eCreateCallback), typeof(Test24));

            StepStateTransitions test24Dict = new StepStateTransitions();
            test24Dict.Add(typeof(eCreateCallback), typeof(Test25));

            StepStateTransitions test25Dict = new StepStateTransitions();
            test25Dict.Add(typeof(eCreateCallback), typeof(Test26));

            StepStateTransitions test26Dict = new StepStateTransitions();
            test26Dict.Add(typeof(eCreateCallback), typeof(Test27));

            StepStateTransitions test27Dict = new StepStateTransitions();
            test27Dict.Add(typeof(eCreateCallback), typeof(Test28));

            StepStateTransitions test28Dict = new StepStateTransitions();
            test28Dict.Add(typeof(eCreateCallback), typeof(Test29));

            StepStateTransitions test29Dict = new StepStateTransitions();
            test29Dict.Add(typeof(eCreateCallback), typeof(Test30));

            StepStateTransitions test30Dict = new StepStateTransitions();
            test30Dict.Add(typeof(eCreateCallback), typeof(Test31));

            StepStateTransitions test31Dict = new StepStateTransitions();
            test31Dict.Add(typeof(eCreateCallback), typeof(Test32));

            StepStateTransitions test32Dict = new StepStateTransitions();
            test32Dict.Add(typeof(eCreateCallback), typeof(Test33));

            StepStateTransitions test33Dict = new StepStateTransitions();
            test33Dict.Add(typeof(eCreateCallback), typeof(Test34));

            StepStateTransitions test34Dict = new StepStateTransitions();
            test34Dict.Add(typeof(eCreateCallback), typeof(Test35));

            StepStateTransitions test35Dict = new StepStateTransitions();
            test35Dict.Add(typeof(eCreateCallback), typeof(Test36));

            StepStateTransitions test36Dict = new StepStateTransitions();
            test36Dict.Add(typeof(eCreateCallback), typeof(Test37));

            StepStateTransitions test37Dict = new StepStateTransitions();
            test37Dict.Add(typeof(eCreateCallback), typeof(Test38));

            StepStateTransitions test38Dict = new StepStateTransitions();
            test38Dict.Add(typeof(eCreateCallback), typeof(Test39));

            StepStateTransitions test39Dict = new StepStateTransitions();
            test39Dict.Add(typeof(eCreateCallback), typeof(Test40));

            StepStateTransitions test40Dict = new StepStateTransitions();
            test40Dict.Add(typeof(eCreateCallback), typeof(Test41));

            StepStateTransitions test41Dict = new StepStateTransitions();
            test41Dict.Add(typeof(eCreateCallback), typeof(Test42));

            StepStateTransitions test42Dict = new StepStateTransitions();
            test42Dict.Add(typeof(eCreateCallback), typeof(Test43));

            StepStateTransitions test43Dict = new StepStateTransitions();
            test43Dict.Add(typeof(eCreateCallback), typeof(Test44));

            StepStateTransitions test44Dict = new StepStateTransitions();
            test44Dict.Add(typeof(eCreateCallback), typeof(Test45));

            StepStateTransitions test45Dict = new StepStateTransitions();
            test45Dict.Add(typeof(eCreateCallback), typeof(Test46));

            StepStateTransitions test46Dict = new StepStateTransitions();
            test46Dict.Add(typeof(eCreateCallback), typeof(Test47));

            StepStateTransitions test47Dict = new StepStateTransitions();
            test47Dict.Add(typeof(eCreateCallback), typeof(Test48));

            StepStateTransitions test48Dict = new StepStateTransitions();
            test48Dict.Add(typeof(eCreateCallback), typeof(Test49));

            StepStateTransitions test49Dict = new StepStateTransitions();
            test49Dict.Add(typeof(eCreateCallback), typeof(Test50));

            StepStateTransitions test50Dict = new StepStateTransitions();
            test50Dict.Add(typeof(eCreateCallback), typeof(Test52));

            StepStateTransitions test51Dict = new StepStateTransitions();
            test51Dict.Add(typeof(eCreateCallback), typeof(Test52));

            StepStateTransitions test52Dict = new StepStateTransitions();
            test52Dict.Add(typeof(eCreateCallback), typeof(Test53));

            StepStateTransitions test53Dict = new StepStateTransitions();
            test53Dict.Add(typeof(eCreateCallback), typeof(Test54));

            StepStateTransitions test54Dict = new StepStateTransitions();
            test54Dict.Add(typeof(eCreateCallback), typeof(Test55));

            StepStateTransitions test55Dict = new StepStateTransitions();
            test55Dict.Add(typeof(eCreateCallback), typeof(Test56));

            StepStateTransitions test56Dict = new StepStateTransitions();
            test56Dict.Add(typeof(eCreateCallback), typeof(Test57));

            StepStateTransitions test57Dict = new StepStateTransitions();
            test57Dict.Add(typeof(eCreateCallback), typeof(Test58));

            StepStateTransitions test58Dict = new StepStateTransitions();
            test58Dict.Add(typeof(eCreateCallback), typeof(Test59));

            StepStateTransitions test59Dict = new StepStateTransitions();
            test59Dict.Add(typeof(eCreateCallback), typeof(End));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Test1), test1Dict);
            dict.Add(typeof(Test2), test2Dict);
            dict.Add(typeof(Test3), test3Dict);
            dict.Add(typeof(Test4), test4Dict);
            dict.Add(typeof(Test5), test5Dict);
            dict.Add(typeof(Test6), test6Dict);
            dict.Add(typeof(Test7), test7Dict);
            dict.Add(typeof(Test8), test8Dict);
            dict.Add(typeof(Test9), test9Dict);
            dict.Add(typeof(Test10), test10Dict);
            dict.Add(typeof(Test11), test11Dict);
            dict.Add(typeof(Test12), test12Dict);
            dict.Add(typeof(Test13), test13Dict);
            dict.Add(typeof(Test14), test14Dict);
            dict.Add(typeof(Test15), test15Dict);
            dict.Add(typeof(Test16), test16Dict);
            dict.Add(typeof(Test17), test17Dict);
            dict.Add(typeof(Test18), test18Dict);
            dict.Add(typeof(Test19), test19Dict);
            dict.Add(typeof(Test20), test20Dict);
            dict.Add(typeof(Test21), test21Dict);
            dict.Add(typeof(Test22), test22Dict);
            dict.Add(typeof(Test23), test23Dict);
            dict.Add(typeof(Test24), test24Dict);
            dict.Add(typeof(Test25), test25Dict);
            dict.Add(typeof(Test26), test26Dict);
            dict.Add(typeof(Test27), test27Dict);
            dict.Add(typeof(Test28), test28Dict);
            dict.Add(typeof(Test29), test29Dict);
            dict.Add(typeof(Test30), test30Dict);
            dict.Add(typeof(Test31), test31Dict);
            dict.Add(typeof(Test32), test32Dict);
            dict.Add(typeof(Test33), test33Dict);
            dict.Add(typeof(Test34), test34Dict);
            dict.Add(typeof(Test35), test35Dict);
            dict.Add(typeof(Test36), test36Dict);
            dict.Add(typeof(Test37), test37Dict);
            dict.Add(typeof(Test38), test38Dict);
            dict.Add(typeof(Test39), test39Dict);
            dict.Add(typeof(Test40), test40Dict);
            dict.Add(typeof(Test41), test41Dict);
            dict.Add(typeof(Test42), test42Dict);
            dict.Add(typeof(Test43), test43Dict);
            dict.Add(typeof(Test44), test44Dict);
            dict.Add(typeof(Test45), test45Dict);
            dict.Add(typeof(Test46), test46Dict);
            dict.Add(typeof(Test47), test47Dict);
            dict.Add(typeof(Test48), test48Dict);
            dict.Add(typeof(Test49), test49Dict);
            dict.Add(typeof(Test50), test50Dict);
            dict.Add(typeof(Test51), test51Dict);
            dict.Add(typeof(Test52), test52Dict);
            dict.Add(typeof(Test53), test53Dict);
            dict.Add(typeof(Test54), test54Dict);
            dict.Add(typeof(Test55), test55Dict);
            dict.Add(typeof(Test56), test56Dict);
            dict.Add(typeof(Test57), test57Dict);
            dict.Add(typeof(Test58), test58Dict);
            dict.Add(typeof(Test59), test59Dict);

            return dict;
        }
    }
}
