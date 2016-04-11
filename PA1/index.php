<!DOCTYPE HTML> 
<html >
<meta charset="utf-8">
<head>
	<title>NBA Player Stats</title>
	<link rel="stylesheet" href="style.css">
</head>

<body> 
	<div class='container-fluid'>
		<!-- input box for user to search nba players -->
		<form>
			<img src='http://vignette1.wikia.nocookie.net/logopedia/images/0/00/NBA-Logo-Vector-PNG1.png/revision/latest?cb=20151104000611' id="nba-logo"></img>
			<input type='text' name='search'></input>
			<button type='submit'>Search</button>
		</form>

		<?php
			// import player class
			require_once('player.php');

			// create PDO to access data from database
			$DB = new PDO('mysql:host=aws-mysql-info344.cgdzkjmtxmog.us-west-2.rds.amazonaws.com;dbname=info344', 'info344user', 'info344sli');

			// querys for user's search and displays results retrieved from the database
			if (isset($_REQUEST['search'])) {
				$search = $_REQUEST['search'];

				// selects all results where user's search is like a nba's first name, last name, or full name 
				$data = $DB->query("SELECT * FROM nbastats WHERE (FirstName LIKE '$search') OR (LastName LIKE '$search') OR (FullName LIKE '$search')")->fetchAll(PDO::FETCH_ASSOC);

				// loops through retrieved data set to display each player's name, team logo, profile image, and stats
				for ($i = 0; $i < sizeof($data); $i++) {
					$player = new Player($data[$i]);
					$name = $player->GetName();
					$imageURL = $player->GetImageURL();
					$errorURL = 'http://i.cdn.turner.com/nba/nba/.element/img/2.0/sect/statscube/players/large/default_nba_headshot_v2.png';
					$logoURL = $player->GetLogoURL();
					$stats = $player->GetStats();
					$statsTitle = array('TEAM', 'PPG', 'GP', '3PTM', 'RB', 'AST', 'STL', 'BLK', 'TO');
					
					echo "<div class='player-info'>";
					echo "<h1 class='player-name'>".$name."</h1>";
					echo "<table>"."<tr>";

					// displays every stats title in the array
					foreach($statsTitle as $statTitle) {
						echo "<th>".$statTitle."</th>";
					}
					echo "</tr>"."<tr>";

					// displays every stat in the array
					foreach ($stats as $stat) {
						echo "<td>".$stat."</td>";
					}
					echo "</tr>"."</table>";
					echo "<img src='$logoURL'>"."</img>";
					echo "<img src={$imageURL} onerror='this.src='{$errorURL}''></img>";
					echo "</div>";
				}
			}
		?>

	</div>
</body>
</html>