# Labbrapport: praktisk laboration

*Kunskapskontroll 2, IT-säkerhet för utvecklare. Fyll i mallen och lämna in som PDF tillsammans med länken till ditt repo. Riktlängd två till tre sidor.*

**Namn:TSOLER HAYITIAN
**Datum:25-08-2026
**Repo (länk till din fork):* https://github.com/TSOLER89/SakerLabb *
**Applikation som analyserades:* SakerLabb Support *

---

## 1. Kort om applikationen och analysen

Beskriv i några meningar vilken app du analyserade, vad den gör och hur du genomförde analysen. Ange vilka verktyg du använde och hur du körde dem (CodeQL default setup med språk C#, ZAP passiv och aktiv skanning mot vilken adress).

*SakerLabb Support är en .NET-applikation för hantering av supportärenden. Applikationen innehåller bland annat funktioner för ärenden, kommentarer, import av XML och JSON samt diagnostikfunktioner.

Jag genomförde både statisk och dynamisk säkerhetsanalys. För den statiska analysen använde jag GitHub CodeQL med Default Setup och språket C#. CodeQL analyserade källkoden och hittade 36 öppna fynd. För den dynamiska analysen körde jag OWASP ZAP mot den lokala applikationen på http://localhost:5080. Jag utforskade applikationen genom ZAP:s proxy och analyserade de passiva larm som skapades.*

---

## 2. Fem fynd

Fyll i tabellen. Minst ett fynd ska komma från statisk analys (CodeQL) och minst ett från dynamisk analys (ZAP). Spara bevis i form av skärmbild eller rapportutdrag och hänvisa till det per fynd.

| Nr | Källa (CodeQL/ZAP) | Regel-id eller alert | Allvarlighet (+ confidence för ZAP) | Fil och rad eller URL | Verkligt eller falskt positivt | Motivering (2–4 meningar) |
|----|--------------------|----------------------|-------------------------------------|-----------------------|--------------------------------|---------------------------|
| 1 | CodeQL | cs/xml/insecure-dtd-handling – Untrusted XML is read insecurely | Critical  |SakerLabb.Web/Services/ImportService.cs:27  | Verkligt | XML-data kommer direkt från användaren via [FromForm] string xml i endpointen /import/xml. CodeQL visar att värdet går vidare utan synlig validering till XML-läsaren samtidigt som osäker DTD-hantering används. Detta kan möjliggöra XXE-relaterade attacker, exempelvis externa resursanrop eller denial of service. |
| 2 | CodeQL | deserialization-untrusted-input – Deserialization of untrusted data | Critical | SakerLabb.Web/Services/ImportService.cs:40 | Verkligt  | JSON-data kommer direkt från användaren via [FromForm] string json och skickas till JsonConvert.DeserializeObject. Deserialiseraren använder TypeNameHandling.All, vilket gör att typinformation från användarens JSON kan påverka vilka .NET-objekt som skapas. Detta kan leda till allvarliga konsekvenser såsom denial of service och i vissa miljöer oönskad kodexekvering. |
| 3 | CodeQL | cs/command-line-injection – Uncontrolled command line | Critical | SakerLabb.Web/Services/ImportService.cs:57 | Verkligt | Parametern host kommer direkt från användaren via [FromQuery] i /diagnostik/ping. Värdet byggs utan synlig validering direkt in i argumenten till cmd.exe. Eftersom användarkontrollerad data når en kommandotolk finns risk för command injection. |
| 4 | ZAP | Content Security Policy (CSP) Header Not Set | Medium, Confidence: High | http://localhost:5080/ | Verkligt  |ZAP visar att flera HTML-sidor, bland annat startsidan och /tickets, saknar Content-Security-Policy-header. Utan CSP saknas ett extra skydd som begränsar vilka skript, stilar och andra resurser webbläsaren får ladda. Detta kan bland annat öka konsekvensen av en XSS-sårbarhet.  |
| 5 | ZAP | Absence of Anti-CSRF Tokens | Medium, Confidence: Low | http://localhost:5080/tickets/6 | Verkligt, med viss osäkerhet | ZAP hittade POST-formulär till /ticket/comment och /ticket/status utan synligt anti-CSRF-token. HTML-svaret innehöll ingen __RequestVerificationToken och controller-metoderna saknade ett synligt [ValidateAntiForgeryToken]. ZAP anger Low confidence, därför har fyndet bedömts genom både ZAP-resultatet och granskning av formulär och kod. |

Bevis (skärmbilder eller utdrag), numrerade efter fyndet ovan:
I folder SakerLabb/Bevis finns skärmbilder och utdrag som visar bevisen för varje fynd. Nedan är en kort beskrivning av bevisen.
*Bevis för fynd 1:
Skärmbild från CodeQL som visar Untrusted XML is read insecurely, Critical, ImportService.cs:27.
Skärmbild från Show paths som visar flödet från [FromForm] string xml till XmlReader.Create.

Bevis för fynd 2:
Skärmbild från CodeQL som visar Deserialization of untrusted data, Critical, ImportService.cs:40.
Skärmbild från Show paths som visar flödet från [FromForm] string json till JsonConvert.DeserializeObject.

Bevis för fynd 3:
Skärmbild från CodeQL som visar Uncontrolled command line, Critical, ImportService.cs:57.
Skärmbild från Show paths som visar flödet från [FromQuery] string host till argumenten för cmd.exe.

Bevis för fynd 4:
Skärmbild från ZAP som visar Content Security Policy (CSP) Header Not Set, Risk Medium och Confidence High på localhost:5080.

Bevis för fynd 5:
Skärmbild från ZAP som visar Absence of Anti-CSRF Tokens, Risk Medium, Confidence Low och formuläret till /ticket/comment.*

---

## 3. Prioritering

Rangordna fynden och motivera ordningen med allvarlighetsgrad, exponering och utnyttjbarhet. Vilket tar du först och varför?

*1. Uncontrolled command line

Jag prioriterar command injection först eftersom fyndet har Critical allvarlighetsgrad och användarkontrollerad data når cmd.exe. Endpointen /diagnostik/ping har inget synligt autentiseringskrav, vilket ger hög exponering. Kombinationen av hög konsekvens, hög exponering och relativt direkt dataflöde gör detta till det mest prioriterade fyndet.

2. Deserialization of untrusted data

Detta fynd prioriteras som nummer två eftersom det också är Critical och tar emot JSON direkt från användaren. TypeNameHandling.All gör deserialiseringen särskilt riskabel eftersom användarinmatning kan påverka typvalet. Konsekvensen kan vara mycket allvarlig, men utnyttjandet är mer beroende av vilka typer och gadget-kedjor som finns tillgängliga.

3. Untrusted XML is read insecurely

XML-fyndet är också Critical och användaren kan själv skicka XML till /import/xml. Den osäkra DTD-hanteringen kan möjliggöra XXE-relaterade attacker. Jag placerar det efter command injection och osäker deserialisering eftersom konsekvensen i den aktuella miljön är mer beroende av vilka externa resurser servern kan nå.

4. Absence of Anti-CSRF Tokens

Fyndet har Medium risk och kan innebära att en användare kan luras att skicka en oönskad POST-request till applikationen. Det prioriteras efter de tre Critical-fynden eftersom konsekvensen generellt är lägre. ZAP anger dessutom Low confidence, vilket gör att fyndet kräver mer manuell verifiering.

5. Content Security Policy Header Not Set

CSP-fyndet har Medium risk och High confidence. Det är ett verkligt konfigurationsproblem, men CSP fungerar främst som ett extra skydd och förhindrar inte i sig den bakomliggande sårbarheten. Därför prioriteras det efter de mer direkt exploaterbara fynden.*

---

## 4. Åtgärder (minst tre)

Använd mönstret nedan per åtgärdat fynd. Varje åtgärd ska gå att spåra tillbaka till ett fynd i tabellen ovan, och beviset efter ska vara en **ny körning av verktyget**, inte din egen kod.

### Åtgärd 1

Åtgärd 1 – Command injection

Fynd:3 – cs/command-line-injection – Uncontrolled command line

Plats: SakerLabb.Web/Services/ImportService.cs, rad 57

Bevis före: CodeQL visade alerten Uncontrolled command line med allvarlighetsgraden Critical. Dataflödesspåret visade att parametern host kom direkt från användaren via [FromQuery] i endpointen /diagnostik/ping och sedan användes i argumenten till cmd.exe.

Bedömning: Jag bedömde fyndet som verkligt. Användarkontrollerad data byggdes direkt in i en kommandosträng som kördes via cmd.exe, utan tillräcklig validering. Detta kunde möjliggöra command injection och därmed påverka vilka kommandon som körs på servern.

Åtgärd: Jag tog bort användningen av cmd.exe och startar i stället ping.exe direkt. Argumenten skickas separat med ProcessStartInfo.ArgumentList, och värdet i host valideras innan processen startas. På så sätt skickas användarens indata inte längre genom ett kommandoskal.

Commit: 9df48c6 – Fix cs/command-line-injection in diagnostics ping

Bevis efter: Efter ändringen kördes CodeQL på nytt på main. Alerten Uncontrolled command line med regel-id cs/command-line-injection visas nu som Fixed.

### Åtgärd 2

Åtgärd 2 – Osäker deserialisering

Fynd: 2 – cs/unsafe-deserialization-untrusted-input – Deserialization of untrusted data

Plats: SakerLabb.Web/Services/ImportService.cs, rad 40

Bevis före: CodeQL visade alerten Deserialization of untrusted data med allvarlighetsgraden Critical. Dataflödesspåret visade att JSON-data kom direkt från användaren via [FromForm] string json och skickades vidare till JsonConvert.DeserializeObject. Deserialiseraren använde dessutom TypeNameHandling.All.

Bedömning: Jag bedömde fyndet som verkligt. Eftersom användaren kunde styra JSON-innehållet samtidigt som TypeNameHandling.All var aktiverat kunde typinformationen i JSON påverka vilka .NET-objekt som skapades. Detta innebär risk för osäker deserialisering och kan beroende på miljön leda till exempelvis denial of service eller annan oönskad kodexekvering.

Åtgärd: Jag tog bort TypeNameHandling.All och användningen av JsonConvert.DeserializeObject för den här användarkontrollerade datan. JSON behandlas i stället som data med System.Text.Json och JsonDocument.Parse. Jag lade även till kontroll så att tom JSON inte accepteras.

Commit: 50c5fbf – Fix cs/unsafe-deserialization-untrusted-input

Bevis efter: Efter ändringen kördes CodeQL på nytt på main. Alerten Deserialization of untrusted data med regel-id cs/unsafe-deserialization-untrusted-input visas nu som Fixed. GitHub visar även att fyndet fixerades via commit 50c5fbf.

### Åtgärd 3

Åtgärd 3 – Osäker XML- och DTD-hantering

Fynd: 1 – cs/xml/insecure-dtd-handling – Untrusted XML is read insecurely

Plats: SakerLabb.Web/Services/ImportService.cs, omkring rad 27–28

Bevis före: CodeQL visade alerten Untrusted XML is read insecurely med allvarlighetsgraden Critical. Dataflödesspåret visade att XML-data kom direkt från användaren via [FromForm] string xml och skickades till XML-parsern. DtdProcessing.Parse och XmlUrlResolver användes, vilket tillät osäker DTD-hantering och externa XML-resurser.

Bedömning: Jag bedömde fyndet som verkligt. Användarkontrollerad XML behandlades med DTD-parsning aktiverad och en extern resolver, vilket kunde möjliggöra XXE-relaterade attacker. En angripare skulle exempelvis kunna försöka få servern att läsa externa resurser eller orsaka denial of service.

Åtgärd: Jag ändrade DtdProcessing från Parse till Prohibit och satte XmlResolver = null både i XmlReaderSettings och i XmlDocument. Därmed tillåts inte längre DTD-behandling eller externa XML-resurser vid import av användarkontrollerad XML.

Commit: 79c090f – Fix cs/xml/insecure-dtd-handling

Bevis efter: Efter ändringen kördes CodeQL på nytt på main. Alerten Untrusted XML is read insecurely med regel-id cs/xml/insecure-dtd-handling visas nu som Fixed. GitHub visar att fyndet fixerades via commit 79c09f0.



## 5. Eventuella bortval

Om du valt att inte åtgärda ett fynd, skriv ned tre saker per bortval: risken, motivet och den kompenserande kontrollen. Sätt gärna ett datum för omprövning.

*Bortval 1 – Content Security Policy (CSP) Header Not Set

Risk:
Utan CSP saknas ett extra skydd mot bland annat XSS.

Motiv:
Fyndet hade Medium risk och prioriterades efter de tre Critical-fynden som var mer direkt utnyttjbara.

Kompenserande kontroll:
ASP.NET Core använder normalt HTML-encoding, vilket minskar vissa XSS-risker.

Bortval 2 – Absence of Anti-CSRF Tokens

Risk:
En angripare kan försöka lura en inloggad användare att skicka oönskade POST-anrop.

Motiv:
Fyndet hade Medium risk och Low confidence och prioriterades därför efter de tre Critical-fynden.

Kompenserande kontroll:
Applikationen använder app.UseAntiforgery(), men POST-endpointsen bör senare kontrolleras och skyddas med tokenvalidering.*
