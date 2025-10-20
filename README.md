
### Original MapEnhancer Mod for Railroader by Vanguard
https://www.nexusmods.com/railroader/mods/18

# How to run this version
1. Install Unity Mod Manager from https://www.nexusmods.com/site/mods/21
2. Download latest release ZIP from https://github.com/lstealth/rr-mapenhancer-mod/releases
3. Run Unity Mod Manager, drop ZIP into Mods

# MapEnhancer v1.6.0 - Recent Changes

## New Features

### Enhanced Map Tooltips (ALT+Hover)
A new feature that displays detailed information when hovering over car/locomotive icons on the map while holding ALT.

**How to Use:**
- Hold down the **ALT key** (left or right)
- Hover over any car or locomotive icon on the map
- A detailed tooltip appears after a brief delay

**Information Displayed:**
- **For All Cars:**
  - Destination status (at destination ✓ or en route →)
  - Train name (if part of a consist)
  - Cargo/load information with percentages
  - Passenger count and destinations
  - Handbrake and hotbox warnings
  
- **For Steam Locomotives:**
  - Current speed (MPH)
  - Coal level with visual progress bar `[██████░░░░]`
  - Water level with visual progress bar
  
- **For Diesel Locomotives:**
  - Current speed (MPH)
  - Diesel fuel level with visual progress bar

**Features:**
- Smart positioning that stays within window bounds
- Performance optimized with 1-second data caching
- Clean, easy-to-read layout with progress bars
- Only appears when ALT is held (no clutter)

### Track Grade Markers
A major new feature that visualizes track elevation changes directly on the map.

**Key Features:**
- **Visual Grade Indicators**: Three colored arrow markers (^, ^^, ^^^) show track slope severity:
  - **Yellow (^)**: Moderate grades (1.0% - 1.7%)
  - **Orange (^^)**: Steep grades (1.7% - 2.5%)
  - **Red (^^^)**: Mountain grades (2.5%+)
  
- **Configurable**:
  - Toggle grade markers on/off via settings UI

**Technical Details:**
- Arrows point in the uphill direction
- Scales with junction marker scale setting
- Grade calculation samples 10 meters ahead and behind each marker point
- Markers are placed every 50 meters along tracks
- Markers are positioned below track lines (at 1500 units) for clear visibility 

### Signal Icon Colorization
Signals on the map now dynamically change color based on their current aspect.

**Color Coding:**
- **Red**: Stop signal
- **Yellow**: Approach or Diverging Approach
- **Green**: Clear or Diverging Clear
- **Orange**: Restricting
- **White**: Default/unknown state

**Implementation:**
- Real-time color updates as signal aspects change
- 80% opacity for better visibility

### Passenger Stop Track Coloring
Passenger station tracks now have their own distinct color on the map (configurable teal/green by default), making it easier to identify passenger facilities separate from industrial spurs.

- **Configurable**:
  - Customize the color for passenger station tracks


## Improvements

### Enhanced Track Classification
- Improved segment classification for passenger stops
- Better distinction between mainline, branch, industrial, and passenger tracks
- More accurate color coding across all track types
- No more track class manipulation; uses existing game data for reliability via calling ColorTrackSegment postfix

## Settings
New configurable options added to the mod settings:
- **Show Track Grade Markers**: Toggle grade markers on/off
- **Track Color - Passenger Stops**: Customize the color for passenger station tracks

Existing settings continue to work with the new features:
- Junction marker scale now also affects grade marker size
- All zoom and visibility settings apply to grade markers

## Version History
- **v1.6.0** (latest): Track grade markers, signal colorization, passenger stop coloring, enhanced ALT+hover tooltips
- **v1.5.3**: Patched TrackClass manipulation, experimental signal colorization, passenger stop coloring
- **v1.5.2.2025**: Hotfix of crash in 2025 Railroader version (Culling crashes)

## Technical Notes
- Grade markers use TextMeshPro for rendering efficiency
- Markers are instantiated from prefabs created at map load
- All new features integrate with existing MapEnhancer culling and optimization systems
- Compatible with Unity Mod Manager 0.32.4+

## Credits
Original MapEnhancer by Vanguard  
Maintained and enhanced by lstealth
