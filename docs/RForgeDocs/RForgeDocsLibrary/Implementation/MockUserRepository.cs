﻿using RForgeDocsLibrary.Repository;
using RForgeDocsLibrary.Services;

namespace RForgeDocsLibrary.Implementation;

public class MockUserRepository : IUserRepository
{
    private List<User> userCollection = [
        #region User List
     new User() { Bio="A passionate artist who finds solace in the beauty of nature, capturing its essence through vibrant brushstrokes.", DateCreated = DateTime.Now.AddMonths(1).AddDays(1/5).AddYears(-1), Username = "EJohnson", Id = 1, FirstName = "Emily", LastName="Johnson", Email = "emily.johnson@example.com" },
         new User() { Bio="A dedicated environmentalist advocating for sustainable practices and protecting our planet's ecosystems.", DateCreated = DateTime.Now.AddMonths(2).AddDays(2/5).AddYears(-2), Username = "MWilliams", Id = 2, FirstName = "Michael", LastName="Williams", Email = "michael.williams@example.net" },
         new User() { Bio="A talented singer-songwriter whose soulful melodies touch the hearts of audiences worldwide.", DateCreated = DateTime.Now.AddMonths(3).AddDays(3/5).AddYears(-3), Username = "SDavis", Id = 3, FirstName = "Sophia", LastName="Davis", Email = "sophia.davis@example.org" },
         new User() { Bio="A tech-savvy entrepreneur who thrives on innovation, constantly pushing boundaries in the digital realm.", DateCreated = DateTime.Now.AddMonths(4).AddDays(4/5).AddYears(-4), Username = "JMiller", Id = 4, FirstName = "Jacob", LastName="Miller", Email = "jacob.miller@example.com" },
         new User() { Bio="A globetrotting travel writer, capturing the essence of diverse cultures and inspiring wanderlust in readers.", DateCreated = DateTime.Now.AddMonths(5).AddDays(5/5).AddYears(-5), Username = "AAnderson", Id = 5, FirstName = "Ava", LastName="Anderson", Email = "ava.anderson@example.net" },
         new User() { Bio="A compassionate social worker, tirelessly championing the rights of marginalized communities.", DateCreated = DateTime.Now.AddMonths(6).AddDays(6/5).AddYears(-6), Username = "WWilson", Id = 6, FirstName = "William", LastName="Wilson", Email = "william.wilson@example.org" },
         new User() { Bio="A culinary wizard, blending flavors and traditions to create exquisite gastronomic experiences.", DateCreated = DateTime.Now.AddMonths(7).AddDays(7/5).AddYears(-7), Username = "IMoore", Id = 7, FirstName = "Isabella", LastName="Moore", Email = "isabella.moore@example.com" },
         new User() { Bio="An intrepid explorer, venturing into uncharted territories and uncovering the mysteries of the natural world.", DateCreated = DateTime.Now.AddMonths(8).AddDays(8/5).AddYears(-8), Username = "DTaylor", Id = 8, FirstName = "Daniel", LastName="Taylor", Email = "daniel.taylor@example.net" },
         new User() { Bio="A gifted photographer, capturing the world's wonders through a unique lens, freezing moments in time.", DateCreated = DateTime.Now.AddMonths(9).AddDays(9/5).AddYears(-9), Username = "MThomas", Id = 9, FirstName = "Mia", LastName="Thomas", Email = "mia.thomas@example.org" },
         new User() { Bio="A dedicated teacher, inspiring young minds and fostering a love for learning.", DateCreated = DateTime.Now.AddMonths(10).AddDays(10/5).AddYears(-10), Username = "EJackson", Id = 10, FirstName = "Ethan", LastName="Jackson", Email = "ethan.jackson@example.com" },
         new User() { Bio="A skilled architect, designing awe-inspiring structures that seamlessly blend form and function.", DateCreated = DateTime.Now.AddMonths(11).AddDays(11/5).AddYears(-11), Username = "OWhite", Id = 11, FirstName = "Olivia", LastName="White", Email = "olivia.white@example.com" },
         new User() { Bio="A passionate fitness enthusiast, motivating others to embrace a healthy and active lifestyle.", DateCreated = DateTime.Now.AddMonths(12).AddDays(12/5).AddYears(-12), Username = "JHarris", Id = 12, FirstName = "Joseph", LastName="Harris", Email = "joseph.harris@example.net" },
         new User() { Bio="An accomplished author, weaving captivating tales that transport readers to fantastical realms.", DateCreated = DateTime.Now.AddMonths(13).AddDays(13/5).AddYears(-13), Username = "EMartin", Id = 13, FirstName = "Emma", LastName="Martin", Email = "emma.martin@example.org" },
         new User() { Bio="A compassionate veterinarian, dedicated to the well-being of animals and advocating for their rights.", DateCreated = DateTime.Now.AddMonths(14).AddDays(14/5).AddYears(-14), Username = "NThompson", Id = 14, FirstName = "Noah", LastName="Thompson", Email = "noah.thompson@example.com" },
         new User() { Bio="A skilled artisan, crafting exquisite handmade items with unwavering attention to detail.", DateCreated = DateTime.Now.AddMonths(15).AddDays(15/5).AddYears(-15), Username = "AGarcia", Id = 15, FirstName = "Abigail", LastName="Garcia", Email = "abigail.garcia@example.net" },
         new User() { Bio="A talented fashion designer, pushing boundaries with bold and innovative creations.", DateCreated = DateTime.Now.AddMonths(16).AddDays(16/5).AddYears(-16), Username = "LMartinez", Id = 16, FirstName = "Liam", LastName="Martinez", Email = "liam.martinez@example.org" },
         new User() { Bio="A passionate conservationist, working tirelessly to protect endangered species and their habitats.", DateCreated = DateTime.Now.AddMonths(17).AddDays(17/5).AddYears(-17), Username = "ARobinson", Id = 17, FirstName = "Avery", LastName="Robinson", Email = "avery.robinson@example.com" },
         new User() { Bio="A skilled pastry chef, crafting decadent desserts that tantalize the senses.", DateCreated = DateTime.Now.AddMonths(18).AddDays(18/5).AddYears(-18), Username = "BClark", Id = 18, FirstName = "Benjamin", LastName="Clark", Email = "benjamin.clark@example.net" },
         new User() { Bio="A dedicated community organizer, bringing people together to create positive change.", DateCreated = DateTime.Now.AddMonths(19).AddDays(19/5).AddYears(-19), Username = "ARodriguez", Id = 19, FirstName = "Amelia", LastName="Rodriguez", Email = "amelia.rodriguez@example.org" },
         new User() { Bio="A gifted musician, mesmerizing audiences with their virtuosic performances.", DateCreated = DateTime.Now.AddMonths(20).AddDays(20/5).AddYears(-20), Username = "MLewis", Id = 20, FirstName = "Mason", LastName="Lewis", Email = "mason.lewis@example.com" },
         new User() { Bio="An adventurous thrill-seeker, constantly seeking new adrenaline-fueled experiences.", DateCreated = DateTime.Now.AddMonths(21).AddDays(21/5).AddYears(-21), Username = "HLee", Id = 21, FirstName = "Harper", LastName="Lee", Email = "harper.lee@example.net" },
         new User() { Bio="A compassionate counselor, providing guidance and support to those in need.", DateCreated = DateTime.Now.AddMonths(22).AddDays(22/5).AddYears(-22), Username = "EWalker", Id = 22, FirstName = "Elijah", LastName="Walker", Email = "elijah.walker@example.org" },
         new User() { Bio="A talented interior designer, transforming spaces into beautiful and functional havens.", DateCreated = DateTime.Now.AddMonths(23).AddDays(23/5).AddYears(-23), Username = "CHall", Id = 23, FirstName = "Charlotte", LastName="Hall", Email = "charlotte.hall@example.com" },
         new User() { Bio="A skilled woodworker, creating intricate and timeless pieces from the finest materials.", DateCreated = DateTime.Now.AddMonths(24).AddDays(24/5).AddYears(-24), Username = "JAllen", Id = 24, FirstName = "James", LastName="Allen", Email = "james.allen@example.net" },
         new User() { Bio="A passionate gardener, cultivating vibrant landscapes and nurturing nature's bounty.", DateCreated = DateTime.Now.AddMonths(25).AddDays(25/5).AddYears(-25), Username = "SYoung", Id = 25, FirstName = "Scarlett", LastName="Young", Email = "scarlett.young@example.org" },
         new User() { Bio="A dedicated social media influencer, inspiring and connecting with a global audience.", DateCreated = DateTime.Now.AddMonths(26).AddDays(26/5).AddYears(-26), Username = "HHernandez", Id = 26, FirstName = "Henry", LastName="Hernandez", Email = "henry.hernandez@example.com" },
         new User() { Bio="A skilled mixologist, crafting innovative and delectable cocktails.", DateCreated = DateTime.Now.AddMonths(27).AddDays(27/5).AddYears(-27), Username = "EKing", Id = 27, FirstName = "Elizabeth", LastName="King", Email = "elizabeth.king@example.net" },
         new User() { Bio="A passionate athlete, pushing physical limits and embodying the spirit of sportsmanship.", DateCreated = DateTime.Now.AddMonths(28).AddDays(28/5).AddYears(-28), Username = "AWright", Id = 28, FirstName = "Alexander", LastName="Wright", Email = "alexander.wright@example.org" },
         new User() { Bio="A gifted artist, breathing life into their canvases with bold strokes and vivid colors.", DateCreated = DateTime.Now.AddMonths(29).AddDays(29/5).AddYears(-29), Username = "SLopez", Id = 29, FirstName = "Sofia", LastName="Lopez", Email = "sofia.lopez@example.com" },
         new User() { Bio="A dedicated philanthropist, using their wealth and influence to create positive change.", DateCreated = DateTime.Now.AddMonths(30).AddDays(30/5).AddYears(-30), Username = "MHill", Id = 30, FirstName = "Matthew", LastName="Hill", Email = "matthew.hill@example.net" },
         new User() { Bio="A skilled mechanic, with an innate understanding of engines and a passion for restoring classic cars.", DateCreated = DateTime.Now.AddMonths(31).AddDays(31/5).AddYears(-31), Username = "LScott", Id = 31, FirstName = "Layla", LastName="Scott", Email = "layla.scott@example.org" },
         new User() { Bio="A talented jeweler, crafting exquisite pieces that capture the beauty of precious gems.", DateCreated = DateTime.Now.AddMonths(32).AddDays(32/5).AddYears(-32), Username = "AGreen", Id = 32, FirstName = "Andrew", LastName="Green", Email = "andrew.green@example.com" },
         new User() { Bio="A passionate advocate for mental health, breaking stigmas and providing support.", DateCreated = DateTime.Now.AddMonths(33).AddDays(33/5).AddYears(-33), Username = "LAdams", Id = 33, FirstName = "Lillian", LastName="Adams", Email = "lillian.adams@example.net" },
         new User() { Bio="A skilled carpenter, building sturdy and beautiful structures with unwavering precision.", DateCreated = DateTime.Now.AddMonths(34).AddDays(34/5).AddYears(-34), Username = "JBaker", Id = 34, FirstName = "Joshua", LastName="Baker", Email = "joshua.baker@example.org" },
         new User() { Bio="A gifted poet, weaving words into evocative tapestries of emotion and imagery.", DateCreated = DateTime.Now.AddMonths(35).AddDays(35/5).AddYears(-35), Username = "NGonzalez", Id = 35, FirstName = "Nora", LastName="Gonzalez", Email = "nora.gonzalez@example.com" },
         new User() { Bio="A passionate skydiver, embracing the thrill of freefall and soaring through the skies.", DateCreated = DateTime.Now.AddMonths(36).AddDays(36/5).AddYears(-36), Username = "LNelson", Id = 36, FirstName = "Lucas", LastName="Nelson", Email = "lucas.nelson@example.net" },
         new User() { Bio="A dedicated scientist, pushing the boundaries of knowledge and seeking groundbreaking discoveries.", DateCreated = DateTime.Now.AddMonths(37).AddDays(37/5).AddYears(-37), Username = "GCarter", Id = 37, FirstName = "Grace", LastName="Carter", Email = "grace.carter@example.org" },
         new User() { Bio="A skilled tailor, creating custom-fitted garments with impeccable attention to detail.", DateCreated = DateTime.Now.AddMonths(38).AddDays(38/5).AddYears(-38), Username = "DMitchell", Id = 38, FirstName = "David", LastName="Mitchell", Email = "david.mitchell@example.com" },
         new User() { Bio="A passionate yoga instructor, guiding others on a journey of mindfulness and self-discovery.", DateCreated = DateTime.Now.AddMonths(39).AddDays(39/5).AddYears(-39), Username = "CPerez", Id = 39, FirstName = "Chloe", LastName="Perez", Email = "chloe.perez@example.net" },
         new User() { Bio="A gifted dancer, captivating audiences with their graceful movements and expressive performances.", DateCreated = DateTime.Now.AddMonths(40).AddDays(40/5).AddYears(-40), Username = "CRoberts", Id = 40, FirstName = "Caleb", LastName="Roberts", Email = "caleb.roberts@example.org" },
         new User() { Bio="A dedicated coach, inspiring and mentoring athletes to reach their full potential.", DateCreated = DateTime.Now.AddMonths(41).AddDays(41/5).AddYears(-41), Username = "PTurner", Id = 41, FirstName = "Penelope", LastName="Turner", Email = "penelope.turner@example.com" },
         new User() { Bio="A skilled florist, arranging vibrant blooms into breathtaking and artistic displays.", DateCreated = DateTime.Now.AddMonths(42).AddDays(42/5).AddYears(-42), Username = "RPhillips", Id = 42, FirstName = "Ryan", LastName="Phillips", Email = "ryan.phillips@example.net" },
         new User() { Bio="A passionate chef, blending flavors and techniques to create unforgettable culinary experiences.", DateCreated = DateTime.Now.AddMonths(43).AddDays(43/5).AddYears(-43), Username = "RCampbell", Id = 43, FirstName = "Riley", LastName="Campbell", Email = "riley.campbell@example.org" },
         new User() { Bio="A dedicated activist, advocating for social justice and human rights causes.", DateCreated = DateTime.Now.AddMonths(44).AddDays(44/5).AddYears(-44), Username = "NParker", Id = 44, FirstName = "Nathan", LastName="Parker", Email = "nathan.parker@example.com" },
         new User() { Bio="A gifted sculptor, breathing life into stone and metal, creating awe-inspiring works of art.", DateCreated = DateTime.Now.AddMonths(45).AddDays(45/5).AddYears(-45), Username = "HEvans", Id = 45, FirstName = "Hazel", LastName="Evans", Email = "hazel.evans@example.net" },
         new User() { Bio="A skilled barista, crafting the perfect cup of coffee with artistry and precision.", DateCreated = DateTime.Now.AddMonths(46).AddDays(46/5).AddYears(-46), Username = "DEdwards", Id = 46, FirstName = "Dylan", LastName="Edwards", Email = "dylan.edwards@example.org" },
         new User() { Bio="A passionate hiker, exploring scenic trails and embracing the beauty of the great outdoors.", DateCreated = DateTime.Now.AddMonths(47).AddDays(47/5).AddYears(-47), Username = "VCollins", Id = 47, FirstName = "Victoria", LastName="Collins", Email = "victoria.collins@example.com" },
         new User() { Bio="A dedicated personal trainer, motivating and guiding clients on their fitness journeys.", DateCreated = DateTime.Now.AddMonths(48).AddDays(48/5).AddYears(-48), Username = "GStewart", Id = 48, FirstName = "Gabriel", LastName="Stewart", Email = "gabriel.stewart@example.net" },
         new User() { Bio="A gifted illustrator, bringing stories and characters to life through captivating visuals.", DateCreated = DateTime.Now.AddMonths(49).AddDays(49/5).AddYears(-49), Username = "ZSanchez", Id = 49, FirstName = "Zoe", LastName="Sanchez", Email = "zoe.sanchez@example.org" },
         new User() { Bio="A skilled makeup artist, transforming faces into stunning works of art.", DateCreated = DateTime.Now.AddMonths(50).AddDays(50/5).AddYears(-50), Username = "LMorris", Id = 50, FirstName = "Logan", LastName="Morris", Email = "logan.morris@example.com" },
         new User() { Bio="A passionate skateboarder, defying gravity with fearless tricks and style.", DateCreated = DateTime.Now.AddMonths(51).AddDays(51/5).AddYears(-51), Username = "LRogers", Id = 51, FirstName = "Lily", LastName="Rogers", Email = "lily.rogers@example.net" },
         new User() { Bio="A dedicated nurse, providing compassionate care and healing to those in need.", DateCreated = DateTime.Now.AddMonths(52).AddDays(52/5).AddYears(-52), Username = "IReed", Id = 52, FirstName = "Isaac", LastName="Reed", Email = "isaac.reed@example.org" },
         new User() { Bio="A gifted calligrapher, creating intricate and elegant lettering with a deft hand.", DateCreated = DateTime.Now.AddMonths(53).AddDays(53/5).AddYears(-53), Username = "ECook", Id = 53, FirstName = "Evelyn", LastName="Cook", Email = "evelyn.cook@example.com" },
         new User() { Bio="A skilled artisan baker, crafting delectable breads and pastries with time-honored techniques.", DateCreated = DateTime.Now.AddMonths(54).AddDays(54/5).AddYears(-54), Username = "LMorgan", Id = 54, FirstName = "Levi", LastName="Morgan", Email = "levi.morgan@example.net" },
         new User() { Bio="A passionate rock climber, scaling towering cliffs with unwavering determination.", DateCreated = DateTime.Now.AddMonths(55).AddDays(55/5).AddYears(-55), Username = "ABell", Id = 55, FirstName = "Aubrey", LastName="Bell", Email = "aubrey.bell@example.org" },
         new User() { Bio="A dedicated historian, preserving and sharing the rich tapestry of our collective past.", DateCreated = DateTime.Now.AddMonths(56).AddDays(56/5).AddYears(-56), Username = "JMurphy", Id = 56, FirstName = "Jayden", LastName="Murphy", Email = "jayden.murphy@example.com" },
         new User() { Bio="A gifted landscape designer, creating harmonious outdoor spaces that blend nature and functionality.", DateCreated = DateTime.Now.AddMonths(57).AddDays(57/5).AddYears(-57), Username = "ABailey", Id = 57, FirstName = "Adalyn", LastName="Bailey", Email = "adalyn.bailey@example.net" },
         new User() { Bio="A skilled glassblower, shaping molten glass into intricate and breathtaking works of art.", DateCreated = DateTime.Now.AddMonths(58).AddDays(58/5).AddYears(-58), Username = "ORivera", Id = 58, FirstName = "Owen", LastName="Rivera", Email = "owen.rivera@example.org" },
         new User() { Bio="A passionate beekeeper, nurturing thriving hives and promoting the importance of pollinators.", DateCreated = DateTime.Now.AddMonths(59).AddDays(59/5).AddYears(-59), Username = "ACooper", Id = 59, FirstName = "Ariana", LastName="Cooper", Email = "ariana.cooper@example.com" },
         new User() { Bio="A dedicated librarian, curating knowledge and fostering a love of reading.", DateCreated = DateTime.Now.AddMonths(60).AddDays(60/5).AddYears(-60), Username = "JRichardson", Id = 60, FirstName = "John", LastName="Richardson", Email = "john.richardson@example.net" },
         new User() { Bio="A gifted ceramist, molding clay into beautiful and functional works of art.", DateCreated = DateTime.Now.AddMonths(61).AddDays(61/5).AddYears(-61), Username = "MCox", Id = 61, FirstName = "Mackenzie", LastName="Cox", Email = "mackenzie.cox@example.org" },
         new User() { Bio="A skilled carpenter, crafting stunning furniture pieces with impeccable craftsmanship.", DateCreated = DateTime.Now.AddMonths(62).AddDays(62/5).AddYears(-62), Username = "SHoward", Id = 62, FirstName = "Samuel", LastName="Howard", Email = "samuel.howard@example.com" },
         new User() { Bio="A passionate cyclist, exploring winding roads and scenic paths on two wheels.", DateCreated = DateTime.Now.AddMonths(63).AddDays(63/5).AddYears(-63), Username = "MWard", Id = 63, FirstName = "Madeline", LastName="Ward", Email = "madeline.ward@example.net" },
         new User() { Bio="A dedicated marine biologist, studying and protecting the vast and diverse ocean ecosystems.", DateCreated = DateTime.Now.AddMonths(64).AddDays(64/5).AddYears(-64), Username = "JTorres", Id = 64, FirstName = "Jack", LastName="Torres", Email = "jack.torres@example.org" },
         new User() { Bio="A gifted quilter, weaving intricate patterns and stories into every stitch.", DateCreated = DateTime.Now.AddMonths(65).AddDays(65/5).AddYears(-65), Username = "KPeterson", Id = 65, FirstName = "Kaylee", LastName="Peterson", Email = "kaylee.peterson@example.com" },
         new User() { Bio="A skilled metalsmith, forging intricate and functional pieces from molten metal.", DateCreated = DateTime.Now.AddMonths(66).AddDays(66/5).AddYears(-66), Username = "LGray", Id = 66, FirstName = "Luke", LastName="Gray", Email = "luke.gray@example.net" },
         new User() { Bio="A passionate birdwatcher, appreciating the beauty and diversity of avian life.", DateCreated = DateTime.Now.AddMonths(67).AddDays(67/5).AddYears(-67), Username = "ARamirez", Id = 67, FirstName = "Alexa", LastName="Ramirez", Email = "alexa.ramirez@example.org" },
         new User() { Bio="A dedicated social worker, empowering individuals and advocating for positive change.", DateCreated = DateTime.Now.AddMonths(68).AddDays(68/5).AddYears(-68), Username = "JJames", Id = 68, FirstName = "Jackson", LastName="James", Email = "jackson.james@example.com" },
         new User() { Bio="A gifted weaver, creating intricate and vibrant textiles through a blend of art and tradition.", DateCreated = DateTime.Now.AddMonths(69).AddDays(69/5).AddYears(-69), Username = "MWatson", Id = 69, FirstName = "Makayla", LastName="Watson", Email = "makayla.watson@example.net" },
         new User() { Bio="A skilled woodcarver, bringing intricate designs to life through the intricate shaping of wood.", DateCreated = DateTime.Now.AddMonths(70).AddDays(70/5).AddYears(-70), Username = "CBrooks", Id = 70, FirstName = "Carter", LastName="Brooks", Email = "carter.brooks@example.org" },
         new User() { Bio="A passionate skier, carving through fresh powder and embracing the thrill of the slopes.", DateCreated = DateTime.Now.AddMonths(71).AddDays(71/5).AddYears(-71), Username = "KKelly", Id = 71, FirstName = "Kennedy", LastName="Kelly", Email = "kennedy.kelly@example.com" },
         new User() { Bio="A dedicated teacher, inspiring young minds and nurturing a love for learning.", DateCreated = DateTime.Now.AddMonths(72).AddDays(72/5).AddYears(-72), Username = "GSanders", Id = 72, FirstName = "Grayson", LastName="Sanders", Email = "grayson.sanders@example.net" },
         new User() { Bio="A gifted mosaic artist, piecing together vibrant patterns and designs from fragments of glass.", DateCreated = DateTime.Now.AddMonths(73).AddDays(73/5).AddYears(-73), Username = "RPrice", Id = 73, FirstName = "Ruby", LastName="Price", Email = "ruby.price@example.org" },
         new User() { Bio="A skilled potter, shaping clay into beautiful and functional works of art.", DateCreated = DateTime.Now.AddMonths(74).AddDays(74/5).AddYears(-74), Username = "CBennett", Id = 74, FirstName = "Callie", LastName="Bennett", Email = "callie.bennett@example.com" },
         new User() { Bio="A passionate surfer, riding the waves with grace and an unwavering connection to the ocean.", DateCreated = DateTime.Now.AddMonths(75).AddDays(75/5).AddYears(-75), Username = "IBarnes", Id = 75, FirstName = "Isaac", LastName="Barnes", Email = "isaac.barnes@example.net" },
         new User() { Bio="A dedicated environmental scientist, studying and protecting our planet's delicate ecosystems.", DateCreated = DateTime.Now.AddMonths(76).AddDays(76/5).AddYears(-76), Username = "SRoss", Id = 76, FirstName = "Savannah", LastName="Ross", Email = "savannah.ross@example.org" },
         new User() { Bio="A gifted woodturner, crafting elegant and functional pieces from carefully shaped wood.", DateCreated = DateTime.Now.AddMonths(77).AddDays(77/5).AddYears(-77), Username = "WHenderson", Id = 77, FirstName = "Wyatt", LastName="Henderson", Email = "wyatt.henderson@example.com" },
         new User() { Bio="A skilled blacksmith, forging intricate and sturdy metal pieces with a hammer and anvil.", DateCreated = DateTime.Now.AddMonths(78).AddDays(78/5).AddYears(-78), Username = "EColeman", Id = 78, FirstName = "Elsie", LastName="Coleman", Email = "elsie.coleman@example.net" },
         new User() { Bio="A passionate spelunker, exploring the depths of underground caves with awe and respect.", DateCreated = DateTime.Now.AddMonths(79).AddDays(79/5).AddYears(-79), Username = "CJenkins", Id = 79, FirstName = "Colton", LastName="Jenkins", Email = "colton.jenkins@example.org" },
         new User() { Bio="A dedicated social entrepreneur, using innovative business models to create positive social impact.", DateCreated = DateTime.Now.AddMonths(80).AddDays(80/5).AddYears(-80), Username = "PPerry", Id = 80, FirstName = "Paisley", LastName="Perry", Email = "paisley.perry@example.com" },
         new User() { Bio="A gifted printmaker, creating unique and captivating works of art through various printmaking techniques.", DateCreated = DateTime.Now.AddMonths(81).AddDays(81/5).AddYears(-81), Username = "LPowell", Id = 81, FirstName = "Luca", LastName="Powell", Email = "luca.powell@example.net" },
         new User() { Bio="A skilled leatherworker, crafting beautiful and functional pieces from high-quality leather.", DateCreated = DateTime.Now.AddMonths(82).AddDays(82/5).AddYears(-82), Username = "SLong", Id = 82, FirstName = "Skylar", LastName="Long", Email = "skylar.long@example.org" },
         new User() { Bio="A passionate rock climber, scaling towering cliffs with unwavering determination and skill.", DateCreated = DateTime.Now.AddMonths(83).AddDays(83/5).AddYears(-83), Username = "JPatterson", Id = 83, FirstName = "Josiah", LastName="Patterson", Email = "josiah.patterson@example.com" },
         new User() { Bio="A dedicated urban planner, designing sustainable and livable communities for the future.", DateCreated = DateTime.Now.AddMonths(84).AddDays(84/5).AddYears(-84), Username = "NHughes", Id = 84, FirstName = "Natalie", LastName="Hughes", Email = "natalie.hughes@example.net" },
         new User() { Bio="A gifted tapestry weaver, creating intricate and vibrant works of art one thread at a time.", DateCreated = DateTime.Now.AddMonths(85).AddDays(85/5).AddYears(-85), Username = "JFlores", Id = 85, FirstName = "Jordan", LastName="Flores", Email = "jordan.flores@example.org" },
         new User() { Bio="A skilled stained glass artist, crafting breathtaking windows and panels with colored glass.", DateCreated = DateTime.Now.AddMonths(86).AddDays(86/5).AddYears(-86), Username = "NWashington", Id = 86, FirstName = "Naomi", LastName="Washington", Email = "naomi.washington@example.com" },
         new User() { Bio="A passionate disc golfer, navigating challenging courses with precision and strategy.", DateCreated = DateTime.Now.AddMonths(87).AddDays(87/5).AddYears(-87), Username = "DButler", Id = 87, FirstName = "Dominic", LastName="Butler", Email = "dominic.butler@example.net" },
         new User() { Bio="A dedicated human rights advocate, fighting for justice and equality for all.", DateCreated = DateTime.Now.AddMonths(88).AddDays(88/5).AddYears(-88), Username = "CSimmons", Id = 88, FirstName = "Camilla", LastName="Simmons", Email = "camilla.simmons@example.org" },
         new User() { Bio="A gifted bookbinder, preserving and showcasing literary works with intricate binding techniques.", DateCreated = DateTime.Now.AddMonths(89).AddDays(89/5).AddYears(-89), Username = "MFoster", Id = 89, FirstName = "Maximus", LastName="Foster", Email = "maximus.foster@example.com" },
         new User() { DateCreated = DateTime.Now.AddMonths(90).AddDays(90/5).AddYears(-90), Username = "AGonzales", Id = 90, FirstName = "Ariel", LastName="Gonzales", Email = "ariel.gonzales@example.net" },
         new User() { DateCreated = DateTime.Now.AddMonths(91).AddDays(91/5).AddYears(-91), Username = "LBryant", Id = 91, FirstName = "Luis", LastName="Bryant", Email = "luis.bryant@example.org" },
         new User() { DateCreated = DateTime.Now.AddMonths(92).AddDays(92/5).AddYears(-92), Username = "AAlexander", Id = 92, FirstName = "Addyson", LastName="Alexander", Email = "addyson.alexander@example.com" },
         new User() { DateCreated = DateTime.Now.AddMonths(93).AddDays(93/5).AddYears(-93), Username = "HRussell", Id = 93, FirstName = "Hudson", LastName="Russell", Email = "hudson.russell@example.net" },
         new User() { DateCreated = DateTime.Now.AddMonths(94).AddDays(94/5).AddYears(-94), Username = "SGriffin", Id = 94, FirstName = "Skyler", LastName="Griffin", Email = "skyler.griffin@example.org" },
         new User() { DateCreated = DateTime.Now.AddMonths(95).AddDays(95/5).AddYears(-95), Username = "ADiaz", Id = 95, FirstName = "Adrian", LastName="Diaz", Email = "adrian.diaz@example.com" },
         new User() { DateCreated = DateTime.Now.AddMonths(96).AddDays(96/5).AddYears(-96), Username = "SHayes", Id = 96, FirstName = "Stella", LastName="Hayes", Email = "stella.hayes@example.net" },
         new User() { DateCreated = DateTime.Now.AddMonths(97).AddDays(97/5).AddYears(-97), Username = "JMyers", Id = 97, FirstName = "Jaxon", LastName="Myers", Email = "jaxon.myers@example.org" },
         new User() { DateCreated = DateTime.Now.AddMonths(98).AddDays(98/5).AddYears(-98), Username = "LFord", Id = 98, FirstName = "Luna", LastName="Ford", Email = "luna.ford@example.com" },
         new User() { DateCreated = DateTime.Now.AddMonths(99).AddDays(99/5).AddYears(-99), Username = "IHamilton", Id = 99, FirstName = "Isaiah", LastName="Hamilton", Email = "isaiah.hamilton@example.net" },
         new User() { DateCreated = DateTime.Now.AddMonths(100).AddDays(100/5).AddYears(-100), Username = "AGraham", Id = 100, FirstName = "Aria", LastName="Graham", Email = "aria.graham@example.org" }
    #endregion
    ];

    public IQueryable<User> GetAllUsers()
    {
        return userCollection.AsQueryable();
    }

    public async Task<User> GetUser(int userId)
    {
        await Task.Delay(100);
        return userCollection.FirstOrDefault(u => u.Id == userId);
    }

    public async Task<bool> HasUserWithUsername(string username)
    {
        await Task.Delay(50);
        return userCollection.Any(u => u.Username == username);
    }

}
