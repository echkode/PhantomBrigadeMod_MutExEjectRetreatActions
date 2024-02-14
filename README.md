# MutExEjectRetreatActions

A library mod for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/) that makes the eject and retreat action buttons mutually exclusive to prevent you from placing the wrong one by accident.

It is compatible with game release version **1.2.1**. It works with both the Steam and Epic installs of the game. All library mods are fragile and susceptible to breakage whenever a new version is released.

I didn't come up with this idea. I found it in a post in the PhantomBrigade forum on the BYG Discord server: https://discord.com/channels/380929397445754890/585971299550101514/1206018004781895721

There are two controlled ways to have a unit exit combat early: eject and retreat. Eject is pretty messy and means you have to later go pick up the pilot; the pilot can even die during eject. Retreat is much nicer but you have to be in a specially marked zone for that action to work.

Both actions are sometimes available simultaneously and the buttons for them are placed right next to each other. It doesn't make sense to have them both available since a unit is either in a retreat zone and therefore retreat is always the better option, or the unit is not in a retreat zone and the only option is to eject.

![Both buttons shown, setting up the possibility of placing the wrong action](https://github.com/echkode/PhantomBrigadeMod_MutExEjectRetreatActions/assets/48565771/f7653506-f840-4375-9ace-5e1fa0f10088)

There's a failsafe on the retreat action which prevents it from doing a retreat if the unit is outside the retreat zone when the action starts in the execution phase. However, there's no such thing for the eject action so you could have a unit eject when it's in the retreat zone.

You should see only the eject button and not the retreat button when the unit is outside the retreat zone as shown in this screenshot.

![No retreat button when outside retreat zone](https://github.com/echkode/PhantomBrigadeMod_MutExEjectRetreatActions/assets/48565771/c255585f-393f-4572-80b5-183eb84f233a)

Conversely, you should see the retreat button and not the eject button when the unit is in the retreat zone.

![Retreat button without the eject button in the retreat zone](https://github.com/echkode/PhantomBrigadeMod_MutExEjectRetreatActions/assets/48565771/a9289010-f16b-4c63-a297-d97b50c90dd8)

This mod extends the two actions by using some new features that have entered the game in the 1.2 series. These are function interfaces that you can implement so no more low-level hooking with Harmony is needed. One interface is called when the game is considering what actions a player unit might have available. The other interface is called when an action is created.

It shouldn't be necessary to implement the action creation function interface but there's a timing bug that prevents the buttons from being updated correctly when a run action into a retreat zone is placed in the planning phase. This means the button doesn't change from eject to retreat like it should when a unit is in a retreat zone.

There are three workarounds for this timing issue.

1. The manual workaround is to click on the unit after you place the run action into the retreat zone. This causes the game to run the action availability logic again and this time it will notice the unit is in the retreat zone and update the action buttons.
2. The reflection workaround uses the action creation function interface to detect if the unit is in a retreat zone when the action is created. In this case, the button is still shows the wrong action but when the action is placed, it is swapped out with the correct action.
3. The low-level workaround uses some Harmony goop to patch in a call to the method that computes the correct path length of a run action. This is the root of the problem and I think it's safe to process the path right after the run action is created but I'm not 100% sure about that.

You can create a `settings.yaml` file in the mod directory. The mod will read that file if it exists when the mod is loaded. There are two fields you can put in the file to enable the reflection and low-level workarounds. Both can be enabled at the same time without any problem but that just wastes cycles.

- `useActionSwap` : if this is `true`, then the reflection workaround is enabled
- `usePatch` : if this is `true`, then the low-level workaround is enabled

Here's a video showing the problem the timing issue creates. You can see that when the unit first enters the retreat zone the eject button doesn't change to be retreat. However, after I click on the unit, the button changes. That's the manual workaround.
<video controls src="https://github.com/echkode/PhantomBrigadeMod_MutExEjectRetreatActions/assets/48565771/9a189b81-5975-4d26-a4a8-cfba79918dc6">
  <p>Run action into retreat zone placed in planning phase doesn't change eject button to retreat</p>
</video>
With the reflection workaround, the button still shows the wrong action but after you place the action, it is swapped out for the correct action. Here's a video showing the eject action changing to a retreat action when the unit is in the retreat zone.
<video controls src="https://github.com/echkode/PhantomBrigadeMod_MutExEjectRetreatActions/assets/48565771/c905e485-1fe4-41bc-97d7-6d64119b8d9c">
  <p>Unit in retreat zone with eject button showing; after clicking on button and placing action, it magically changes to a retreat action</p>
</video>
Here's a video showing the unit moving out of the retreat zone and placing a retreat action that is swapped with an eject action. The unit can't retreat outside the zone.
<video controls src="https://github.com/echkode/PhantomBrigadeMod_MutExEjectRetreatActions/assets/48565771/04f5d1c2-fd3c-40aa-b896-57d2d94bca8d">
  <p>Unit moves out of retreat zone and button stays as a retreat action; after clicking on button and placing action, it magically changes to an eject action</p>
</video>
With the low-level workaround, everything just works. Here's a video showing the button switching properly when the unit runs into the retreat zone.
<video controls src="https://github.com/echkode/PhantomBrigadeMod_MutExEjectRetreatActions/assets/48565771/09df72c0-3799-4d67-8840-4bb4ca00bb4a">
  <p>Unit enters retreat zone and the button automatically switches to retreat</p>
</video>

## Technical Notes

The timing issue is with the order of the systems in `CombatSystems`. The availability logic is run in the `CombatUISystems` feature as the player interacts with the UI. A run action is created in `InputCombatPathDrawing` which ends up triggering `InputUILinkModeSync`. Up to this point, the path length of the run action is correct because it's being taken from the painted path. When `InputUILinkModeSync` runs, though, it removes the painted path because the game is no longer in the path painting mode and then calls `CIViewCombatAtion.RefreshSelectedUnitActions()`.

One of the things that refreshing actions does is call the action validation functions. My validation function is called properly but when it goes to get the end position of the selected units movement actions, it gets the wrong value. The path in the newly created run action has to be processed by the `PathLinker` system to get the correct end position. Unfortunately, `PathLinker` runs before `CombatUISystems` so we have to wait until the next frame for the path to be processed. By then, the logic to refresh the actions is complete so my validation function never gets called again with the correct path length.

My low-level patch calls the function that `PathLinker` uses to process paths. I think it's safe to do so because it only processes movement actions for the unit that's passed to it. My validation function passes the selected unit since that's the unit whose actions we want refreshed.

However, there appears to be some sort of background processing that the AstarPathfindingProject Unity asset does and so I don't know if I'm fixing one timing issue by introducing another one.
