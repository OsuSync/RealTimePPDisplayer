# Interlude
This is a Sync! plugin for displaying the current beatmap's pp in real-time when you are playing osu!, the possibilities with this plugin are endless! It seems to be commonly used a streaming plugin.

# Notice!
Please read the README about [osu!RTDP](https://github.com/KedamaOvO/OsuRTDataProvider-Release) and read the [osu! forum post](https://osu.ppy.sh/community/forums/topics/685031) before you use this.

# Request 
1. [osu!Sync](https://github.com/Deliay/osuSync)
2. [osu!RTDataProvider](https://github.com/KedamaOvO/OsuRTDataProvider-Release)
3. [ctb-server(optional)](https://github.com/OsuSync/RealTimePPDisplayer/releases/download/v1.5.0/ctb-server.7z)

# Settings
The settings for this application are located in the config.ini<br>

| Setting Name  | Default Value | Description |
|:------------- |:-------------|:-----|
| OutputMethods | wpf | The output mode of plugin, you can choose "wpf","mmf" and "text" (segmenting with ',' e.g: wpf, text) |
| UseText  | False | Output to txt file (**recommended OutputMethods**) |
| TextOutputPath  | rtpp{0}.txt |  Output file path |
| DisplayHitObject | True  | Whether to display hitobjects (like 300_count/50_count and others) |
| PPFontSize | 48 | pp value text font size (in px) |
| PPFontColor | FFFFFFFF | pp value text color (ARGB Hex  code and no prefix '#') |
| HitObjectFontSize | 24 | Hitobjects (300, 100, 50, miss) text font size (in px) |
| HitObjectFontColor | FFFFFFFF | Hitobjects text color (ARGB Hex value with no prefix '#') |
| BackgroundColor | FF00FF00 | Backgound color (default is green and good for colorkey in OBS) |
| WindowHeight | 172 | Window Height (in px) |
| WindowWidth | 280 | Window Width (in px) |
| SmoothTime | 200 | Time in ms to smooth the pp counter's changes |
| FPS | 60 | FPS |
| Topmost | False | Make the pp window is located at the topmost portion of the screen (you can right click pp window) |
| WindowTextShadow | True| Whether to apply text shadow effect |
| DebugMode | False | Enable debug ouput |
| RoundDigits | 2 | Accurate up to {**RoundDigits**} decimal places. |
| PPFormat | ${rtpp}pp | You can choose **rtpp rtpp_aim rtpp_speed rtpp_acc fcpp fcpp_aim fcpp_speed fcpp_acc maxpp maxpp_aim maxpp_speed maxpp_acc** [more](https://github.com/KedamaOvO/RealTimePPDisplayer/wiki/How-to-customize-my-output-content%3F)|
| HitCountFormat | ${n100}x100 ${n50}x50 ${nmiss}xMiss | You can choose **combo maxcombo fullcombo n300 n100 n50 nmiss** [more](https://github.com/KedamaOvO/RealTimePPDisplayer/wiki/How-to-customize-my-output-content%3F)|
| FontName | Segoe UI | Font name |
| IgnoreTouchScreenDecrease | False | Ignore TD Mod|
| RankingSendPerformanceToChat | False | Send pp to irc chat in the rank interface (If IRC is connected)|

# How to use MMF?
1. Install [obs-text-rtpp-mmf](https://github.com/KedamaOvO/RealTimePPDisplayer/releases/download/v1.1.1/obs-text-rtpp-mmf.7z) to OBS (20.1.3).
2. Add **TextGDIPlusMMF** to scene.
3. Right click **TextGDIPlusMMF**,select **Properties**.
4. Find **Memory mapping file name**,input **rtpp**.(if tourney is enable,input rtpp{id}, e.g. rtpp0)
5. Add **mmf** to **OutputMethods** in **config.ini**, save config.ini.

**Memory mapping file name** = **内存映射文件名** = **記憶體對應檔案** = **MMF.Name**

# Reference
1. [oppai-ng](https://github.com/Francesco149/oppai-ng)
2. [catch-the-pp](https://github.com/osufx/catch-the-pp)

# Preview
Here's the plugin in use on the [Tourney client](https://www.youtube.com/watch?v=begp3yimqaI)!
