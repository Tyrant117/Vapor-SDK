# Vapor-SDK
A collection of tools to for developing application in Unity.

- [Keys](#keys)
- [Events](#events)
- [Observable Values](#observable-values)
- [State Machine](#state-machine)
- [Networking](#networking)
- [Inspector](#inspector)

## Keys
The backbone of many of the systems. A set of classes to create simple, stable, and unique integer key values.

## Events
An global event system for tying events to the key system.
Also contains expanded functionality for working with a provider based component system similar to dependancy injection to make sure data is where it needs to be when its needed.

## Observable Values
A wrapper on primitive types and some core unity types that track when values are changed and optionally fires events when they are.
Also contains a system for tying these values to a larger Observable Class to allow for grouped tracking of data.

## State Machine
A simple state machine system that is code first and can optionally handle layers for running multiple machines at once.

## Networking
A networking system with the KCP and SteamP2P as optional transport layers. Shares similarities to Mirror, but is made the way I like it.
Has interest management, automatic partial state synchronization, and snapshot interpolation.
Has expanded functionality for MMO-like account and backend management.

## Inspector
An Odin-like custom inspector system fully running in the new Unity UI Toolkit. The backbone of custom drawers for the rest of the sdk.