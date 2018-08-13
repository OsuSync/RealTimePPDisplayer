# Profile
This is a Sync! plugin for displaying the current beatmap's pp in real-time when you are playing osu!, you can use this for streaming or etc!

# Notice
Please read the README about [Osu!RTDP](https://github.com/KedamaOvO/OsuRTDataProvider-Release) and [OsuForum](https://osu.ppy.sh/forum/t/685031) before you use this.

# Settings
The settings for this application are located in the config.ini<br>

| Setting Name  | Default Value | Description |
|:------------- |:-------------|:-----|
| OutputMethods | wpf | The output mode of plugin, you can choose "wpf","mmf" and "text" (segmenting with ',' e.g: wpf,text) |
| UseText  | False | Wether to output to txt file(**recommended OutputMethods**) |
| TextOutputPath  | rtpp{0}.txt |  Output file path |
| DisplayHitObject | True  | Wether to display hitobjects (like 300_count/50_count and others) |
| PPFontSize | 48 | pp value text font size(pt) |
| PPFontColor | FFFFFFFF | pp value text color (ARGB Hex  code and no prefix '#') |
| HitObjectFontSize | 24 | Hitobjects text font size(pt) |
| HitObjectFontColor | FFFFFFFF | Hitobjects text color (ARGB Hex  code and no prefix '#') |
| BackgroundColor | FF00FF00 | Backgound color (default is green and good for colorkey in OBS) |
| WindowHeight | 172 | Window Height(px) |
| WindowWidth | 280 | Window Width(px) |
| SmoothTime | 200 | Time in ms to smooth the pp counter changing |
| FPS | 60 | FPS |
| Topmost | False | Whether to pp Window is located at the topmost portion of the screen (you can right click pp window) |
| WindowTextShadow | True|Wether to apply text shadow effect |
| DebugMode | False | Enable debug ouput |
| RoundDigits | 2 | accurate up to {**RoundDigits**} decimal places. |
| PPFormat | ${rtpp}pp | you can choose **rtpp rtpp_aim rtpp_speed rtpp_acc fcpp fcpp_aim fcpp_speed fcpp_acc maxpp maxpp_aim maxpp_speed maxpp_acc** [more](https://github.com/KedamaOvO/RealTimePPDisplayer/wiki/How-to-customize-my-output-content%3F)|
| HitCountFormat | ${n100}x100 ${n50}x50 ${nmiss}xMiss | you can choose **combo maxcombo fullcombo n300 n100 n50 nmiss** [more](https://github.com/KedamaOvO/RealTimePPDisplayer/wiki/How-to-customize-my-output-content%3F)|
| FontName | Segoe UI | Font name |

# How to use MMF?
1. Install [obs-text-rtpp-mmf](https://github.com/KedamaOvO/RealTimePPDisplayer/releases/download/v1.1.1/obs-text-rtpp-mmf.7z) to OBS (20.1.3).
2. Add **TextGDIPlusMMF** to scene.
3. Right click **TextGDIPlusMMF**,select **Properties**.
4. Find **Memory mapping file name**,input **rtpp**.(if tourney is enable,input rtpp{id}, e.g. rtpp0)
5. Add **mmf** to **OutputMethods** in **config.ini**, save config.ini.

**Memory mapping file name** = **内存映射文件名** = **記憶體對應檔案** = **MMF.Name**

# Request 
1. [Osu!Sync](https://github.com/Deliay/osuSync)
2. [OsuRTDataProvider](https://github.com/KedamaOvO/OsuRTDataProvider-Release)

# Preview
Tourney Mode: [Youtube](https://www.youtube.com/watch?v=begp3yimqaI)
