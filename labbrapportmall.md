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

```
Fynd:        (nr och regel-id/alert från tabellen ovan)
Plats:       (fil och rad, eller URL)
Bevis före:  (skärmbild eller rapportutdrag som visar fyndet)
Bedömning:   (verkligt eller falskt positivt, kort motiverat)
Åtgärd:      (vad du ändrade, med commit-hash)
Bevis efter: (ny körning: CodeQL-alerten står som Fixed, eller ZAP-larmet är borta ur den nya rapporten)
```

### Åtgärd 2

```
Fynd:
Plats:
Bevis före:
Bedömning:
Åtgärd:
Bevis efter:
```

### Åtgärd 3

```
Fynd:
Plats:
Bevis före:
Bedömning:
Åtgärd:
Bevis efter:
```

---

## 5. Eventuella bortval

Om du valt att inte åtgärda ett fynd, skriv ned tre saker per bortval: risken, motivet och den kompenserande kontrollen. Sätt gärna ett datum för omprövning.

*Skriv här, eller skriv "inga bortval".*
