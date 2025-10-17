using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UI.Menu;

public class MarkovRailroadNameGenerator
{
	private readonly Dictionary<string, Dictionary<string, int>> _chain = new Dictionary<string, Dictionary<string, int>>();

	private const string InitialKey = "_initial";

	private static readonly HashSet<string> RailroadNames = new HashSet<string>
	{
		"A&R Terminal Railroad", "Aberdeen and Rockfish Railroad", "Aberdeen, Carolina and Western Railway", "Acadiana Railway", "Adams-Warnock Railway", "Adrian and Blissfield Rail Road", "Affton Terminal Services Railroad", "Ag Valley Railroad", "Aiken Railway", "Airlake Terminal Railway",
		"Akron Barberton Cluster Railway", "Alabama and Gulf Coast Railway", "Alabama and Tennessee River Railway", "Alabama Export Railroad", "Alabama Southern Railroad", "Alabama Warrior Railway", "Alamo Gulf Coast Railroad", "Alamo North Texas Railroad", "Alaska Railroad", "Albany and Eastern Railroad",
		"Albany Port Railroad", "Alexander Railroad", "Aliquippa and Ohio River Railroad", "Allegheny Valley Railroad", "Allentown and Auburn Railroad", "Alton and Southern Railway", "AN Railway", "Angelina and Neches River Railroad", "Ann Arbor Railroad", "Apache Railway",
		"Appalachian and Ohio Railroad", "Arcade and Attica Railroad", "Arizona and California Railroad", "Arizona Central Railroad", "Arizona Eastern Railway", "Arkansas–Oklahoma Railroad", "Arkansas and Missouri Railroad", "Arkansas Midland Railroad", "Arkansas Southern Railroad", "Arkansas, Louisiana and Mississippi Railroad",
		"Ashland Railway", "Ashtabula, Carson and Jefferson Railroad", "AT&L Railroad", "Atlantic and Western Railway", "Austin Western Railroad", "Autauga Northern Railroad", "B&H Rail Corporation", "Baja California Railroad", "Baldwin City & Southern Railroad", "Ballard Terminal Railroad",
		"Baton Rouge Southern Railroad", "Batten Kill Railroad", "Bauxite and Northern Railway", "Bay Colony Railroad", "Bay Line Railroad", "Bayway Terminal Switching Company", "Beech Mountain Railroad", "Bee Line Railroad", "Bellingham International Railroad", "Belt Railway of Chicago",
		"Belvidere and Delaware River Railway", "Bessemer and Lake Erie Railroad", "BG&CM Railroad", "Big 4 Terminal Railroad", "Big Spring Rail System", "Bighorn Divide and Wyoming Railroad", "Birmingham Terminal Railway", "Bi-State Development Agency", "Belpre Industrial Parkersburg Railroad", "Black River and Western Corporation",
		"Blacklands Railroad", "Blackwell Northern Gateway Railroad", "Bloomer Line", "Blue Rapids Railway", "Blue Ridge Southern Railroad", "BNSF Railway", "Bogalusa Bayou Railroad", "Boise Valley Railroad", "Boone and Scenic Valley Railroad", "Border Transload & Transfer",
		"Border Pacific Railroad", "Brandywine Valley Railroad", "Branford Steam Railroad", "Brookhaven Rail", "Brownsville and Rio Grande International Railroad", "Buckingham Branch Railroad", "Bucyrus Industrial Railroad", "Buffalo and Pittsburgh Railroad", "Buffalo Southern Railroad", "Burlington Junction Railway",
		"Butte, Anaconda and Pacific Railway", "C&NC Railroad", "Caldwell County Railroad", "California Northern Railroad", "Camden and Southern Railroad", "Camp Chase Railway", "Canadian National Railway", "Canadian Pacific Railway", "Caney Fork and Western Railroad", "Canton Railroad",
		"Cape Fear Railways", "Cape May Seashore Lines", "Carolina Coastal Railway", "Carolina Piedmont Railroad", "Cascade and Columbia River Railroad", "CaterParrott Railnet", "CBEC Railroad", "Cedar Rapids and Iowa City Railway", "Central California Traction Company", "Central Indiana and Western Railroad",
		"Central Maine and Quebec Railway", "Central Midland Railway", "Central Montana Rail, Inc.", "Central New England Railroad", "Central New York Railroad", "Central Oregon and Pacific Railroad", "Central Railroad of Indiana", "Central Railroad of Indianapolis", "Central Washington Railroad", "CG Railway",
		"Charlotte Southern Railroad", "Chattahoochee Industrial Railroad", "Chattooga and Chickamauga Railway", "Chesapeake and Albemarle Railroad", "Chesapeake and Indiana Railroad", "Chessie Logistics", "Chestnut Ridge Railroad", "Chicago–Chemung Railroad", "Chicago Heights Terminal Transfer", "Chicago Junction Railway",
		"Chicago Port & Rail", "Chicago Port Railroad", "Chicago Rail Link", "Chicago South Shore and South Bend Railroad", "Chicago, Fort Wayne and Eastern Railroad", "Chicago, St. Paul & Pacific Railroad", "Cicero Central Railroad", "Cimarron Valley Railroad", "Cincinnati Eastern Railroad", "City of Prineville Railway",
		"Clackamas Valley Railroad", "Clarendon and Pittsford Railroad", "Cleveland and Cuyahoga Railroad", "Cleveland Port Railroad", "Cleveland Works Railway", "Clinton Terminal Railroad", "Cloquet Terminal Railroad", "CMC Railroad", "Coffeen and Western Railroad", "Colorado and Wyoming Railway",
		"Columbia and Cowlitz Railway", "Columbia Basin Railroad", "Columbia Business Center Railroad", "Columbia Terminal Railroad", "Columbia & Walla Walla Railway", "Columbus and Chattahoochee Railroad", "Columbus and Greenville Railway", "Columbus and Ohio River Rail Road", "Commonwealth Railway", "Conecuh Valley Railroad",
		"Connecticut Southern Railroad", "Coopersville and Marne Railway", "Coos Bay Rail Link", "Copper Basin Railway", "Corpus Christi Terminal Railroad", "Crab Orchard and Egyptian Railroad", "CSX Transportation", "Dakota & Iowa Railroad", "D&W Railroad", "Dakota Northern Railroad",
		"Dakota Southern Railway", "Dakota, Missouri Valley and Western Railroad", "Dallas, Garland and Northeastern Railroad", "Dardanelle and Russellville Railroad", "De Queen and Eastern Railroad", "Decatur Central Railroad", "Decatur & Eastern Illinois Railroad", "Decatur Junction Railway", "Delaware–Lackawanna Railroad", "Delray Connecting Railroad",
		"Delmarva Central Railroad", "Delta Southern Railroad", "Delta Valley and Southern Railway", "Denver Rock Island Railroad", "Depew, Lancaster and Western Railroad", "Deseret Power Railroad", "Detroit Connecting Railroad", "Dover and Delaware River Railroad", "Dover & Rockaway River Railroad", "Dubois County Railroad",
		"East Brookfield and Spencer Railroad", "East Camden and Highland Railroad", "East Cooper and Berkeley Railroad", "East Erie Commercial Railroad", "East Jersey Railroad", "East Penn Railroad", "East Tennessee Railway", "East Troy Electric Railroad", "Eastern Alabama Railway", "Eastern Idaho Railroad",
		"Eastern Illinois Railroad", "Eastern Kentucky Railway Company", "Eastern Maine Railway", "Effingham Railroad", "El Dorado and Wesson Railway", "Elizabethtown Industrial Railroad", "Elk River Railroad", "Elkhart and Western Railroad", "Ellis and Eastern Company", "Escalante Western Railway",
		"Escanaba and Lake Superior Railroad", "Evansville Western Railway", "Everett Railroad", "Fairfield Southern Company", "Falls Road Railroad", "Farmrail Corporation", "FFG&C Railroad", "Finger Lakes Railway", "First Coast Railroad", "Flats Industrial Railroad",
		"Florida Central Railroad", "Florida East Coast Railway", "Florida Gulf & Atlantic Railroad", "Florida Midland Railroad", "Florida Northern Railroad", "Fordyce and Princeton Railroad", "Fore River Transportation Corporation", "Fort Smith Railroad", "Fort Worth and Western Railroad", "Foxville and Northern Railroad",
		"Fredonia Valley Railroad", "FreightCar Short Line", "FTRL Railway", "Fulton County Railroad", "Fulton County Railway", "Galveston Railroad", "Garden City Western Railway", "Gardendale Railroad", "Gary Railway", "Gateway Eastern Railway",
		"Gateway Industrial Railroad", "Geaux Geaux Railroad", "Georges Creek Railway", "Georgetown Railroad", "Georgia and Florida Railway", "Georgia Central Railway", "Georgia Northeastern Railroad", "Georgia Southern Railway", "Georgia Southwestern Railroad", "Georgia Woodlands Railroad",
		"Gettysburg and Northern Railroad", "Global Container Terminals New York", "Golden Isles Terminal Railroad", "Golden Triangle Railroad", "Goose Lake Railway", "Grafton and Upton Railroad", "Grainbelt Corporation", "Grand Elk Railroad", "Grand Rapids Eastern Railroad", "Grand River Railway",
		"Great Lakes Central Railroad", "Great Northwest Railroad", "Great Walton Railroad", "Great Western Railway of Colorado", "Green Mountain Railroad", "Greenville and Western Railway", "Grenada Railway", "Gulf Coast Switching", "Hainesport Secondary Railroad", "Hampton and Branchville Railroad",
		"Hartwell Railroad", "Heart of Georgia Railroad", "Heritage Railroad", "Herrin Railroad", "High Point, Thomasville & Denton Railroad", "Hilton & Albany Railroad", "Hondo Railway", "Hoffman Transportation", "Hoosier Southern Railroad", "Housatonic Railroad",
		"Huron and Eastern Railway", "Hussey Terminal Railroad", "Idaho and Sedalia Transportation Company", "Idaho Northern and Pacific Railroad", "Illini Terminal Railroad", "Illinois and Midland Railroad", "Illinois Railway", "Illinois Western Railroad", "Indiana and Ohio Railway", "Indiana Eastern Railroad",
		"Indiana Harbor Belt Railroad", "Indiana Northeastern Railroad", "Indiana Rail Road", "Indiana Southern Railroad", "Indiana Southwestern Railway", "Iowa Interstate Railroad", "Iowa & Middletown Railway", "Iowa Northern Railway", "Iowa River Railroad", "Iowa Southern Railway",
		"Iowa Traction Railroad", "Ithaca Central Railroad", "Jackson & Lansing Railroad", "Jacksonville Port Terminal Railroad", "Joppa and Eastern Railroad", "Juniata Terminal Company", "Juniata Valley Railroad", "Kanawha River Railroad", "Kanawha River Terminal", "Kankakee, Beaverville and Southern Railroad",
		"Kansas and Oklahoma Railroad", "Kansas City Southern Railway", "Kansas City Transportation Lines", "Kaw River Railroad", "Kendallville Terminal Railway", "Kennewick Terminal Railroad", "Kentucky and Tennessee Railway", "Keokuk Junction Railway", "Kiamichi Railroad", "Kingman Terminal Railroad",
		"Kinston and Snow Hill Railroad", "Kiski Junction Railroad", "Klamath Northern Railway", "KM Railways", "Knoxville and Holston River Railroad", "Kodak Park Railroad", "KWT Railway", "Kyle Railroad", "Lake Michigan and Indiana Railroad", "Lake State Railway",
		"Lake Superior and Ishpeming Railroad", "Lake Terminal Railroad", "Lancaster and Chester Railway", "Landisville Railroad", "Lapeer Industrial Railroad", "LaSalle Railway", "Laurinburg and Southern Railroad", "Leetsdale Industrial Terminal Railway", "Lehigh Railway", "Lehigh Valley Rail",
		"Lewisburg and Buffalo Creek Railroad", "Little Kanawha River Railroad", "Little Rock and Western Railway", "Little Rock Port Authority Railroad", "Live Oak Railroad", "Livonia, Avon and Lakeville Railroad", "Longview Switching Company", "Lorain Northern Railroad", "Los Angeles Junction Railway", "Louisiana and Delta Railroad",
		"Louisiana and North West Railroad", "Louisiana Southern Railroad", "Louisville and Indiana Railroad", "Lubbock and Western Railway", "Lucas Oil Rail Lines", "Luxapalila Valley Railroad", "Luzerne and Susquehanna Railway", "Lycoming Valley Railroad", "M&B Railroad", "Madison Railroad",
		"Mahoning Valley Railway", "Maine Northern Railway", "Manning Rail", "Manufacturers' Junction Railway", "Marquette Rail, LLC", "Maryland and Delaware Railroad", "Maryland Midland Railway", "Massachusetts Central Railroad", "Massachusetts Coastal Railroad", "Massena Terminal Railroad",
		"Meeker Southern Railroad", "Meridian Southern Railway", "MG Rail, Inc.", "Michigan Shore Railroad", "Michigan Southern Railroad", "Mid-Michigan Railroad", "Middletown and Hummelstown Railroad", "Middletown and New Jersey Railroad", "Milford-Bennington Railroad", "Mineral Range Railroad",
		"Minnesota Commercial Railway", "Minnesota Northern Railroad", "Minnesota Prairie Line, Inc.", "Minnesota, Dakota and Western Railway", "Mission Mountain Railroad", "Mississippi Central Railroad", "Mississippi Delta Railroad", "Mississippi Export Railroad", "Mississippi Southern Railroad", "Mississippi Tennessee Railroad",
		"Itawamba Mississippian Railway", "Missouri and Northern Arkansas Railroad", "Missouri North Central Railroad", "Modesto and Empire Traction Company", "Mohawk, Adirondack and Northern Railroad", "Montana Rail Link", "Morristown and Erie Railway", "Moscow, Camden and San Augustine Railroad", "Mount Hood Railroad", "Mount Vernon Terminal Railway",
		"Napoleon, Defiance & Western Railroad", "Nashville and Eastern Railroad", "Nashville and Western Railroad", "Natchez Railway", "Naugatuck Railroad Company", "Navajo Mine Railroad", "Northampton Switching Company", "Nebraska Central Railroad", "Nebraska Kansas Colorado Railway", "Nebraska Northwestern Railroad",
		"Nevada Industrial Switch", "New Castle Industrial Railroad", "New Century AirCenter Railroad", "New England Central Railroad", "New England Southern Railroad", "New Hampshire Central Railroad", "New Hampshire Northcoast Corporation", "New Hope and Ivyland Railroad", "New Jersey Rail Carriers, LLC", "New Jersey Seashore Lines",
		"New Orleans and Gulf Coast Railway", "New Orleans Public Belt Railroad", "New York and Atlantic Railway", "New York and Lake Erie Railroad", "New York and Ogdensburg Railway", "New York New Jersey Rail, LLC", "New York, Susquehanna and Western Railway", "Newburgh and South Shore Railroad", "Nittany and Bald Eagle Railroad", "Norfolk and Portsmouth Belt Line Railroad",
		"Norfolk Southern Railway", "North Carolina and Virginia Railroad", "North Louisiana & Arkansas Railroad", "North Shore Railroad", "Northern Lines Railway", "Northern Ohio and Western Railway", "Northern Plains Railroad", "Northwestern Oklahoma Railroad", "Northwestern Pacific Railroad", "Oakland Global Rail Enterprise",
		"Ogeechee Railway", "Ohi-Rail Corporation", "Ohio Central Railroad", "Ohio South Central Railroad", "Ohio Southern Railroad", "Ohio Terminal Railway", "Oil Creek and Titusville Lines, Inc.", "Old Augusta Railroad", "Olympia and Belmore Railroad", "Omaha, Lincoln and Beatrice Railway",
		"Ontario Central Railroad", "Ontario Midland Railroad", "Orange Port Terminal Railway", "Oregon Eastern Railroad", "Oregon Pacific Railroad", "Oregon Railconnect", "Otter Tail Valley Railroad", "Ouachita Railroad", "Owego and Harford Railway", "Ozark Valley Railroad",
		"Pacific Harbor Line, Inc.", "Pacific Sun Railroad", "Paducah and Illinois Railroad", "Paducah and Louisville Railway", "Palouse River and Coulee City Railroad", "Pan Am Railways", "Pan Am Southern", "Panhandle Northern Railroad", "Patriot Woods Railroad", "Pecos Valley Southern Railway",
		"Pend Oreille Valley Railroad", "Peninsula Terminal Company", "Pennsylvania and Southern Railway", "Pennsylvania Southwestern Railroad", "Perry County Railroad", "Peru Industrial Railroad", "Pickens Railway, Honea Path Division", "Pioneer Industrial Railroad", "Pioneer Valley Railroad", "Pittsburgh and Ohio Central Railroad",
		"Pittsburgh, Allegheny and McKees Rocks Railroad", "Plainsman Switching Company", "Plainview Terminal Company", "Point Comfort and Northern Railway", "Port Bienville Short Line Railroad", "Port Harbor Railroad", "Port Jersey Railroad", "Port Manatee Railroad", "Port of Beaumont Railroad", "Port of Catoosa Terminal Railroad",
		"Port of Muskogee Railroad", "Port of Palm Beach District", "Port of Tucson Railroad", "Port Terminal Railroad Association", "Port Terminal Railroad of South Carolina", "Portland and Western Railroad", "Portland Terminal Railroad", "Portland Vancouver Junction Railroad", "Prescott and Northwestern Railroad", "Providence and Worcester Railroad",
		"Puget Sound and Pacific Railroad", "Quincy Railroad", "Quinwood Coal Company", "Rainer Rail", "Rawhide Short Line", "Rapid City, Pierre and Eastern Railroad", "Raritan Central Railway", "Reading Blue Mountain and Northern Railroad", "Red River Valley and Western Railroad", "Republic N&T Railroad",
		"Riceboro Southern Railway", "Richmond Pacific Railroad", "Rio Valley Switching Company", "Ripley & New Albany Railroad", "Riverport Railroad", "Rochester and Southern Railroad", "Rock and Rail LLC", "Rockdale, Sandow and Southern Railroad", "Rogue Valley Terminal Railroad", "Republic Short Line",
		"S&L Railroad", "Sabine River and Northern Railroad", "Sacramento Southern Railroad", "Sacramento Valley Railroad", "Salt Lake, Garfield and Western Railway", "San Antonio Central Railroad", "San Diego and Imperial Valley Railroad", "San Francisco Bay Railway", "San Joaquin Valley Railroad", "San Manuel Arizona Railroad",
		"San Luis and Rio Grande Railroad", "San Luis Central Railroad", "San Pedro Valley Railroad", "Santa Teresa Southern Railroad", "Sand Springs Railway", "Sandersville Railroad", "Santa Cruz, Big Trees and Pacific Railway", "Santa Maria Valley Railroad", "Saratoga and North Creek Railroad", "Savage Bingham and Garfield Railroad",
		"Savannah Port Terminal Railroad", "Seaview Transportation Company", "Seminole Gulf Railway", "SEMO Port Railroad", "Sequatchie Valley Switching Company", "Shamokin Valley Railroad", "Shenandoah Valley Railroad", "Sierra Northern Railway", "Sisseton Milbank Railroad", "SJRE Railroad",
		"SMS Rail Lines of New York, LLC", "SMS Rail Service, Inc.", "South Branch Valley Railroad", "South Buffalo Railway", "South Carolina Central Railroad", "South Central Florida Express, Inc.", "South Central Tennessee Railroad", "South Chicago and Indiana Harbor Railway", "South Kansas and Oklahoma Railroad", "South Plains Lamesa Railroad",
		"South Plains Switching, Ltd", "Southern California Railroad", "Southern Electric Railroad Company", "Southern Indiana Railway", "Southern Railroad of New Jersey", "Southern Switching Company", "Southwest Gulf Railroad", "Southwest Pennsylvania Railroad", "Southwestern Railroad", "Squaw Creek Southern Railroad",
		"St. Croix Valley Railroad", "St. Lawrence and Atlantic Railroad", "St. Maries River Railroad", "St. Mary's Railroad", "St. Marys Railway West", "Stillwater Central Railroad", "St. Paul and Pacific Northwest Railroad", "St. Paul and Pacific Railroad", "Stockton Public Belt Railway", "Stockton Terminal and Eastern Railroad",
		"Stourbridge Railroad", "Strasburg Rail Road", "Swan Ranch Railroad", "Tacoma Rail", "Tacoma Rail Mountain Division", "Tazewell and Peoria Railroad", "Temple and Central Texas Railway", "Tennessee Southern Railroad", "Tennken Railroad", "Terminal Railroad Association of St. Louis",
		"Terminal Railway Alabama State Docks", "Texas & New Mexico Railroad", "Texas and Eastern Railroad", "Texas and Northern Railway", "Texas and Oklahoma Railroad", "Texas Central Business Lines Corporation", "Texas City Terminal Railway", "Texas North Western Railway", "Texas Northeastern Railroad", "Texas Pacifico Transportation",
		"Texas Rock Crusher Railway", "Texas South-Eastern Railroad", "Texas, Gonzales and Northern Railway", "Thermal Belt Railway", "Three Notch Railroad", "Timber Rock Railroad", "Toledo Junction Railroad", "Toledo, Peoria and Western Railway", "Tomahawk Railway", "Tradepoint Rail",
		"Trona Railway", "Tulsa–Sapulpa Union Railway", "Turners Island, LLC", "Twin Cities and Western Railroad", "Tyburn Railroad", "Tyner Terminal Railway", "Union City Terminal Railroad", "Union County Industrial Railroad", "Union Pacific Railroad", "Union Railroad",
		"Upper Merion and Plymouth Railroad", "Utah Railway", "Utah Central Railway", "V&S Railway", "Valdosta Railway", "Vandalia Railroad", "Ventura County Railroad", "Vermilion Valley Railroad", "Vermont Railway", "Vicksburg Southern Railroad",
		"Virginia Southern Railroad", "Wabash Central Railroad", "Walking Horse Railroad", "Wallowa Union Railroad Authority", "Warren and Saline River Railroad", "Warren and Trumbull Railroad", "Washington and Idaho Railway", "Washington County Railroad", "Washington Eastern Railroad", "Washington Royal Line",
		"Wellsboro and Corning Railroad", "West Belt Railway", "West Isle Line", "West Michigan Railroad", "West Shore Railroad Corporation", "West Tennessee Railroad", "Western Maryland Scenic Railroad", "Western New York and Pennsylvania Railroad", "Western Rail Road", "Western Washington Railroad",
		"Wheeling and Lake Erie Railway", "White Deer and Reading Railroad", "Wichita Terminal Association", "Wichita, Tillman and Jackson Railway", "Willamette Valley Railway", "Wilmington Terminal Railroad", "Wilmington and Western Railroad", "Winchester and Western Railroad", "Winston-Salem Southbound Railway", "Wiregrass Central Railroad",
		"Wisconsin and Southern Railroad", "Wisconsin Great Northern Railroad", "Wisconsin Northern Railroad", "Wolf Creek Railroad", "Yakima Central Railroad", "Yadkin Valley Railroad", "Yellowstone Valley Railroad", "York Railway", "Youngstown and Austintown Railroad", "Youngstown and Southeastern Railroad",
		"Youngstown Belt Railroad", "Yreka Western Railroad", "Algoma Central Railway", "Atchison, Topeka and Santa Fe Railway", "Atlantic Coast Line Railroad", "Auto-Train Corporation", "Baltimore and Ohio Railroad", "Bangor and Aroostook Railroad", "Beaumont, Sour Lake & Western Railroad", "Boston and Albany Railroad",
		"Boston and Maine Corporation", "Buffalo Bayou, Brazos & Colorado", "Burlington, Cedar Rapids and Northern Railway", "Burlington Northern Railroad", "Canadian Northern Railway", "Central of Georgia Railway", "Central Pacific Railroad", "Central Railroad of New Jersey", "Central Vermont", "Chesapeake and Ohio Railway",
		"Chicago, Burlington and Quincy Railroad", "Chicago, Milwaukee, St. Paul and Pacific Railroad", "Chicago Great Western Railway", "Chicago, Rock Island and Pacific Railroad", "Chicago and North Western Transportation Company", "Cincinnati, Jackson and Mackinaw Railroad", "Cincinnati, Saginaw, and Mackinaw Railroad", "Colorado and Southern Railway", "Columbia Tap Railway", "Denver and Rio Grande Western Railroad",
		"Detroit, Toledo and Milwaukee Railroad", "Detroit and Pontiac Railroad", "Detroit, Grand Haven and Milwaukee Railway", "Detroit, Toledo & Ironton Railroad", "Erie Railroad", "Florida Overseas Railroad", "Galveston, Harrisburg and San Antonio Railroad", "Grand Trunk", "Great Northern Railway", "Gulf, Colorado and Santa Fe Railway",
		"Fernley and Lassen Railway", "Fredericksburg and Northern Railway", "Hudson Bay Railway", "Houston Belt & Terminal Railroad", "Houston, East & West Texas Railroad", "Houston & Texas Central Railroad", "Illinois Central Railroad", "Inter-California Railway", "Intercolonial Railway of Canada", "International–Great Northern Railroad",
		"LaPorte Houston Northern Railway", "Lackawanna Railroad", "Louisville and Nashville Railroad", "Maine Central", "Minneapolis and St. Louis Railway", "Missouri–Kansas–Texas Railroad", "Missouri Pacific Railroad", "National Transcontinental Railway", "New Orleans and Carrollton Railroad", "New Orleans, Jackson and Great Northern Railroad",
		"New Orleans & Nashville Railroad", "New York Central", "New York, New Haven and Hartford Railroad", "New York, Ontario and Western Railroad", "New York, Chicago and St. Louis Railroad", "Norfolk and Western Railway", "Northern Pacific Railway", "Northern Railway of Canada", "Penn Central", "Pennsylvania Railroad",
		"Pontchartrain Railroad", "Pontiac and Detroit Railroad", "Prince Edward Island Railway", "Raleigh and Gaston Railroad", "Reading Railroad", "St. Louis, Brownsville & Mexico Railroad", "San Antonio & Aransas Pass Railroad", "Seaboard Air Line Railroad", "Southern Pacific Transportation Company", "Spokane, Portland and Seattle Railway",
		"Texas and New Orleans Railroad", "Toledo, Ann Arbor, and North Michigan Railway Company", "Toledo, Saginaw, and Mackinaw Railroad", "Trinity & Brazos Valley Railroad", "Tuckerton Railroad", "Tuskegee Railroad", "Warren and Ouachita Valley", "Western Maryland Railway", "Western Pacific Railroad", "Wilmington and Raleigh Railroad",
		"Wisconsin Central Ltd."
	};

	public MarkovRailroadNameGenerator(int length = 4)
	{
		_chain["_initial"] = new Dictionary<string, int>();
		foreach (string railroadName in RailroadNames)
		{
			string text = railroadName + "$";
			for (int i = 0; i < text.Length - length; i++)
			{
				string key = text.Substring(i, length);
				string key2 = text.Substring(i + 1, length);
				Dictionary<string, int> dictionary3;
				if (!_chain.ContainsKey(key))
				{
					Dictionary<string, int> dictionary = (_chain[key] = new Dictionary<string, int>());
					dictionary3 = dictionary;
				}
				else
				{
					dictionary3 = _chain[key];
				}
				if (i == 0)
				{
					if (!_chain["_initial"].ContainsKey(key))
					{
						_chain["_initial"][key] = 1;
					}
					else
					{
						_chain["_initial"][key]++;
					}
				}
				if (!dictionary3.ContainsKey(key2))
				{
					dictionary3[key2] = 1;
				}
				else
				{
					dictionary3[key2]++;
				}
			}
		}
	}

	private string SelectRandomItem(Dictionary<string, int> items)
	{
		int num = UnityEngine.Random.Range(0, items.Sum((KeyValuePair<string, int> item) => item.Value));
		foreach (KeyValuePair<string, int> item in items)
		{
			item.Deconstruct(out var key, out var value);
			string result = key;
			int num2 = value;
			num -= num2;
			if (num < 0)
			{
				return result;
			}
		}
		throw new Exception();
	}

	public string Generate()
	{
		string text = SelectRandomItem(_chain["_initial"]);
		StringBuilder stringBuilder = new StringBuilder(text);
		while (true)
		{
			if (!_chain.ContainsKey(text))
			{
				throw new ArgumentException("Tuple " + text + " not found in chain");
			}
			text = SelectRandomItem(_chain[text]);
			char c = text.Last();
			if (c == '$')
			{
				break;
			}
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}
}
