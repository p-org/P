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
  UserMachine
};

implementation impl[main = UserMachine]: (compose UserModule, OSModule, OSRDriverModule);
