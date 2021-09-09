## unity lua modding

just some basic wrapper functions and organization around Moonsharp that remove the tedious boiler-plate code and allow modding support to begin easily


#### goals

 - runtime model importing w/ PBR materials, animations
 - custom player models( automatically map to animations, and ragdoll )
 - add 'modded' branch to sNet and integrate this modding framework with multiplayer

 ###### Functionality

```lua
--		   (amt, duration)
ScreenShake(1.2, 0.2)
```