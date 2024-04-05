module OSModule =
{
  LEDMachine -> LEDInterface,
  TimerMachine -> TimerInterface,
  SwitchMachine -> SwitchInterface
};

module OSRDriverModule =
{
  OSRDriverMachine -> OSRDriverInterface
};

module UserModule =
{
  Main
};

implementation DefaultImpl[main = Main]: (compose UserModule, OSModule, OSRDriverModule);
