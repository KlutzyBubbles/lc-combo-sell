# ComboSell

The company is rapidly expanding its collection and is willing to add multipliers to sets of items. 

## Issues (incompatiblities)

Currently multiplier sets DON'T support multiple of the same item, this may be fixed if it is requested otherwise it will be a 'feature'

Any other issues feel free to [submit an issue](https://github.com/KlutzyBubbles/lc-combo-sell/issues/new) or contact me directly via the [Lethal Company Modding Discord](https://discord.gg/XeyYqRdRGC) or PM `@KlutzyBubbles` always trying to make sure the mod is as stable as possible.

## Configuration

Configuration for item combos has been setup in JSON form as the BepinEx config file format is limited in the types that could be used

JSON Config schema

```JSON
{
    "multiplesFirst": true, // Whether to process sets or multiples first
    "includeMultiples": ["string"], // Item names to include when calculating multiples
    "excludeMultiples": ["string"], // Item names to exclude from calculating multiples
    "maxMultiple": 5, // Max amount of duplicates to consider a multiple
    "minMultiple": 1, // Min amount of duplicates to consider a multiple
    "defaultMultipleMultiplier": 0.1, // Used when multipleMultipliers value cant be found
    "defaultSetMultiplier": 0.1, // Used when setMultipliers multiplier key cant be found
    // both use the calculation: multiplier = 1 + (defaultMultipleMultiplier * (itemCount - 1))
    "multipleMultipliers": { // List of key value where key is the itemCount and value is the multiplier
        "2": 2.2,
        "3": 3.3
    },
    "setMultipliers": {
        "Set Name": { // Set name is used when displaying the sold items
            "items": ["string"], // Items that are required to activate this set
            "multiplier": 1.5 // multiplier applied to the total value of the set
        }
    }
}
```

Warning messages will be printed to the console if there are any unknown item names, the example below has `KnownBad` which is an item that doesn't exist. This is to demonstrate how the mod will ignore names / sets with unknown items in them.

Below is an example configuration file. This mod uses the internal item name instead of the display names. Turning on the `debug` option with debug logs enabled in bepinex will print available options to the console.

``` json
{
    "multiplesFirst": true,
    "includeMultiples": [],
    "excludeMultiples": [
        "RobotToy",
        "KnownBad"
    ],
    "maxMultiple": 5,
    "minMultiple": 2,
    "defaultMultipleMultiplier": 0.2,
    "defaultSetMultiplier": 0.2,
    "multipleMultipliers": {
        "2": 1.12,
        "3": 1.2,
        "4": 1.4
    },
    "setMultipliers": {
        "Mask Set": {
            "items": [
                "TragedyMask",
                "ComedyMask"
            ],
            "multiplier": 1.5
        },
        "Unknown Set": {
            "items": [
                "KnownBad",
                "Cog1"
            ],
            "multiplier": 1.5
        },
        "Horny Set": {
            "items": [
                "Airhorn",
                "ClownHorn"
            ],
            "multiplier": 1.69
        }
    }
}
```

## Changelog

See [releases](https://github.com/KlutzyBubbles/lc-combo-sell/releases) section on the github page for changelogs