## About UAV: Radar Enemies Detection

![image](https://cdn.rustpluginshub.com/unsafe/400x400/filters:format(webp)/https://rustpluginshub.com/images/creators/yac-vaguer/plugins/uav-radar-enemies-detection/uav-radar-enemies-detection-cover.webp)

UAV: Radar Enemies Detection call a F15 to that will activate a radar during a certain period of time where you can be able to see where are the enemies located.

This enemies can be represented with different colors and symbols that you can change in the configuration.


You have three types of marks

- Team mates
- NPCs
- Other Players (Enemies)

The plugin is fully configurable

## Commands

### Chat Command

`/uav` and `/uav {steamId}` You can send an UAV  to you (if you are an admin) or to a player given the Steam Id 

### Console Command 

`uav {steamId}` You can send an UAV to a player given the Steam Id 


## Configuration example 

```
{
  "UAV Settings": {
    "Duration (seconds)": 180.0,
    "Radius": 40.0,
    "Skin ID": 3248057023,
    "Warmup Time (seconds)": 5.0,
    "Item Name": "UAV Signal",
    "Tracked Icon URL": "https://cdn.rustpluginshub.com/unsafe/50x50/https://rustpluginshub.com/icons/location.png",
    "Tracked Icon Position (AnchorMin)": "0.006 0.485",
    "Tracked Icon Position (AnchorMax)": "0.105 0.518",
    "Panel Color": "0.96 0.31 0.26 0.47",
    "Text Color": "1 1 1 1"
  },
  "Jet Settings": {
    "Altitude": 200.0,
    "Spawn Distance": 500.0,
    "Duration (seconds)": 15.0
  },
  "Loot Settings": {
    "Enable Loot Drops": true,
    "Loot Containers and Drop Chances": {
      "crate_normal": 0.0,
      "crate_normal_2": 0.0,
      "crate_elite": 2.0,
      "heli_crate": 5.0,
      "bradley_crate": 5.0
    }
  },
  "Debug Mode": true
}
 ```

## Ideas on how to make the UAV Spawn in your server 

1. Add The UAV in the Market if you have one 
2. Add as a part of the Loot in the Raidable Bases or Custom loot 
3. Add as a Skill in the Skill Tree Plugin More here
4. Make the UAV part of the /kits 
5. Add the UAV in vending machines 

Skins from the community that you are free to use besides the default one 

@Dead Nasty https://steamcommunity.com/sharedfiles/filedetails/?id=3248306153

@Mabel https://steamcommunity.com/sharedfiles/filedetails/?id=3233756487

@Mr.Wild https://steamcommunity.com/sharedfiles/filedetails/?id=3247990388


## Contributing

We welcome contributions from the Rust community! Whether it’s fixing a bug, suggesting improvements, or adding new features, your help is appreciated.

### How to Contribute

1. Fork the repository: Start by creating your own copy of the project.
2. Create a new branch: Make a new branch for your feature or bug fix.
3. Make your changes: Implement your code changes and test them thoroughly.
4. Submit a pull request: Open a pull request, providing a clear explanation of what your changes do and why they’re necessary.

### Reporting Issues

If you encounter bugs or have feature suggestions, feel free to report them in the Issues section. Please provide as much detail as possible to help us understand and resolve the issue.

## License

This project is licensed under the MIT License. You are free to use, modify, and distribute this software, as long as the original copyright notice and this permission notice appear in all copies.

```
MIT License

Copyright (c) 2024 Yac Vaguer

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
