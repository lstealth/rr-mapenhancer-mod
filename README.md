# MapEnhancer v1.6.0 - Recent Changes

## Overview
This document describes the new features and improvements added to MapEnhancer between version 1.5.2 (commit a4ecc7f) and version 1.6.0 (latest).

## New Features

### Track Grade Markers
A major new feature that visualizes track elevation changes directly on the map.

**Key Features:**
- **Visual Grade Indicators**: Three colored arrow markers (^, ^^, ^^^) show track slope severity:
  - **Yellow (^)**: Moderate grades (0.5% - 1.0%)
  - **Orange (^^)**: Steep grades (1.0% - 1.5%)
  - **Red (^^^)**: Very steep grades (1.5%+)
  
- **Intelligent Placement**: 
  - Markers are placed every 50 meters along tracks
  - Arrows point in the uphill direction
  - Markers are positioned below track lines (at 1500 units) for clear visibility
  
- **Configurable**:
  - Toggle on/off via settings UI
  - Scales with junction marker scale setting
  - Uses culling groups for performance optimization

**Technical Details:**
- Grade calculation samples 10 meters ahead and behind each marker point
- Handles both positive (uphill) and negative (downhill) grades
- Semi-transparent markers (30% opacity) to avoid cluttering the map
- Respects map window visibility and zoom levels

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
- Automatic monitoring via `SignalIconColorizer` component

### Passenger Stop Track Coloring
Passenger station tracks now have their own distinct color on the map (configurable teal/green by default), making it easier to identify passenger facilities separate from industrial spurs.

## Improvements

### Enhanced Track Classification
- Improved segment classification for passenger stops
- Better distinction between mainline, branch, industrial, and passenger tracks
- More accurate color coding across all track types

### Performance Optimizations
- Culling groups implemented for grade markers to improve rendering performance
- Optimized marker visibility updates based on camera position
- Efficient grade calculation algorithm

## Settings
New configurable options added to the mod settings:
- **Show Track Grade Markers**: Toggle grade markers on/off
- **Track Color - Passenger Stops**: Customize the color for passenger station tracks

Existing settings continue to work with the new features:
- Junction marker scale now also affects grade marker size
- All zoom and visibility settings apply to grade markers

## Version History
- **v1.6.0** (latest): Track grade markers, signal colorization, passenger stop coloring
- **v1.5.3**: Refinements to grade marker icons and rendering
- **v1.5.2.2025**: Base version with enhanced map features

## Technical Notes
- Grade markers use TextMeshPro for rendering efficiency
- Markers are instantiated from prefabs created at map load
- All new features integrate with existing MapEnhancer culling and optimization systems
- Compatible with Unity Mod Manager 0.32.4+

## Credits
Original MapEnhancer by Vanguard  
Maintained and enhanced by lstealth
