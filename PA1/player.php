<!-- Player class for a individual NBA player -->

<?php
	class Player {
		private $name;
		private $imageURL;
		private $logoURL;
		private $stats;

		// uses provided data to set individual player instance's full name, profile image URL, team logo URL, and stats
		function __construct($data) {
			$this->name = $data['FullName'];
			$this->imageURL = 'http://i.cdn.turner.com/nba/nba/.element/img/2.0/sect/statscube/players/large/'.strtolower(str_replace(' ', '_', $this->name)).'.png';
			$this->logoURL = 'http://a.espncdn.com/combiner/i?img=/i/teamlogos/nba/500/'.strtolower($data['Team']).'.png&h=100&w=100';
			$this->stats = array($data['Team'], $data['PPG'], $data['GP'], $data['3PT_M'], $data['RB_Tot'], $data['Ast'], $data['Stl'], $data['Blk'], $data['TO']);
		}

		// returns player's full name
		public function GetName() {
			return $this->name;
		}

		// returns player's profile image URL
		public function GetImageURL() {		
			return $this->imageURL;	
		}

		// returns player's team logo URL
		public function GetLogoURL() {
			return $this->logoURL;
		}

		// returns the player's Team, PPG, GP, 3PTM, RB, AST, STL, BLK, and TO stats
		public function GetStats() {
			return $this->stats;
		}
	}
?>
