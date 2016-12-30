module OSModule 
private;
{
  LEDMachine, 
  TimerMachine,
  SwitchMachine
}

module OSRDriverModule 
private;
{
  OSRDriverMachine
}

module UserModule 
private;
{
  UserMachine
}

implementation (rename UserMachine to Main in UserModule) || OSModule || OSRDriverModule;
