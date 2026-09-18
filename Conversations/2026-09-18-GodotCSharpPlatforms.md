# Conversation: Godot C# platforms

## User

what platforms can a godot 4.7.2 .net c# game run on

## Assistant

I’ll check Godot’s documentation for C# export support and the version you named.

## Assistant

A **Godot 4.7.2 .NET/C# game** can target:

| Platform        | Support                                                     |
| --------------- | ----------------------------------------------------------- |
| Windows         | Supported                                                   |
| Linux           | Supported                                                   |
| macOS           | Supported                                                   |
| Android         | Supported, but C# support is still experimental              |
| iOS / iPadOS    | Supported, but experimental; exporting requires a Mac        |
| Web browsers    | **No official C# export support**                           |

These are the platforms listed in [Godot 4.7’s C# documentation](https://docs.godotengine.org/en/4.7/tutorials/scripting/c_sharp/index.html#c-platform-support).

**Consoles (PlayStation, Xbox, Nintendo Switch)** require a separate port through a third-party provider or your own licensed integration. C# support depends on the provider; they are not built-in export targets. See [Godot’s console support](https://godotengine.org/consoles/).

## User

how do i get godot 4.7.2 gdscript game running on consoles

## Assistant

I’ll check the current console porting options and what they require for a Godot 4.7.2 GDScript project.

## Assistant

**You can keep your game in GDScript.** To run it on consoles, you need an approved console developer account and a console-specific Godot port/export toolchain. The standard Godot download does not include console exports. [Godot’s console guide](https://godotengine.org/consoles/)

**Check 4.7.2 compatibility first:** W4’s current FAQ explicitly lists Godot **4.6, 4.5 and 4.4**, despite broader wording elsewhere on its page. I could not verify a ready-made **4.7.2** port. Ask the provider whether it supports your exact version, renderer and plugins before purchasing or changing your project. [W4 Consoles](https://www.w4games.com/w4consoles)

Your practical options are:

| Route                       | What you do                                                                                         |
| --------------------------- | --------------------------------------------------------------------------------------------------- |
| **W4 Consoles**              | License its console tools and do the port yourself. Supports PS5, Xbox Series X/S and Switch; Switch 2 is advertised as beta. |
| **RAWRLAB’s Nintendo port**  | Use its GDScript-only Switch/Switch 2 port. Requires Nintendo approval and “Godot Ecosystem Contributor” eligibility for no-cost access. No GDExtension support or free support. |
| **Hire a porting studio**    | Pay a provider to adapt and test your game. Godot lists providers including Pineapple Works and other studios. |

Sources: [W4](https://www.w4games.com/w4consoles), [RAWRLAB](https://www.rawrlab.com/godot_nintendo_switch_free_port.html), [Godot’s provider directory](https://godotengine.org/consoles/).

For the self-porting route, the process is:

1. **Choose a console and apply for access:** [Nintendo Developer Portal](https://developer.nintendo.com/), [PlayStation Partners](https://partners.playstation.net/), or [ID@Xbox](https://developer.microsoft.com/en-us/games/).
2. **Get platform approval, SDK access and development hardware.** Registering an account alone does not complete this process.
3. **Obtain a compatible Godot console port.** Follow its private instructions to install/build the engine and export templates.
4. **Export and deploy to the development hardware.** Adapt your game to the console’s platform requirements and test it there.
5. **For a public release**, complete the platform’s submission/certification process, age ratings and store setup. Getting a build running is an earlier milestone. [Godot’s process overview](https://godotengine.org/consoles/)

My suggested first step is to **choose one target console and confirm 4.7.2 support with a provider**. You generally keep your GDScript gameplay code; the work is in the console toolchain, platform integration, performance and release requirements.

## User

what is the cost of each option

## Assistant

I’ll check the published prices and distinguish the tool costs from porting services and console development costs.

## Assistant

The published costs are below. **These are tool/license prices; they do not cover doing the port or the complete console release.**

| Option                 | Published cost                                    | Eligibility / scope                                              |
| ---------------------- | ------------------------------------------------- | ---------------------------------------------------------------- |
| **W4 Starter**         | **$800/year per console**, or **$2,000/year for all** | Up to 30 people; company revenue/funds raised below $300,000       |
| **W4 Pro**             | **$4,000/year per console**, or **$10,000/year for all** | Up to 30 people; revenue/funds raised above $300,000            |
| **W4 Enterprise**      | **Custom quote**                                  | Larger companies, bespoke terms, permanent-license buyout option |
| **RAWRLAB tools**      | **$0 access fee**                                 | Switch/Switch 2; Nintendo approval and contributor eligibility    |
| **Hire a porting studio** | **Project-specific quote**                     | No published fixed price found for Pineapple Works               |

Sources: [W4 pricing](https://www.w4games.com/w4consoles), [RAWRLAB](https://www.rawrlab.com/godot_nintendo_switch_free_port.html), [Pineapple Works](https://pineapple.works/).

A few details affect the actual cost:

- **W4:** No revenue share. Switch and Switch 2 count as separate platforms. Optional premium support is **$7,000/year**, available to Pro/Enterprise customers. Its FAQ says customers using a porting house need Enterprise licensing.
- **W4 renewal:** Starter/Pro are annual subscriptions. After expiry, repository access and permission for new releases/content updates end; the FAQ allows updates adding no new game content using the last code received. [W4 terms summary](https://www.w4games.com/w4consoles)
- **RAWRLAB:** Contributor eligibility can come from code contributions, promotion, sponsorship or donations. Its latest policy specifies **no fixed minimum donation**. Support and consulting are paid separately. [Access policy](https://www.rawrlab.com/priv_no_nda/RAWRLAB%20Games%20-%20Accessing%20our%20Nintendo%20Switch%20Godot%20Engine%20ports.pdf)
- **Other costs:** Budget separately for development hardware, porting/testing work and release requirements. Godot lists devkit pricing as confidential, so there is no reliable public all-in hardware price to add here. [Godot console guide](https://godotengine.org/consoles/)

For a qualifying small developer doing the work themselves, that means **$800/year for one W4 platform**, or **no RAWRLAB tool fee for Nintendo**, plus those additional costs. **Confirm Godot 4.7.2 compatibility before paying**—W4’s published version list still stops at 4.6.

## User

why does it cost money to get godot games running on console, but it's free to get them running on mobile and pc platforms

## Assistant

I’ll check how Godot explains the difference between its desktop/mobile exports and console ports.

## Assistant

**Because console manufacturers restrict development access, the console version of Godot has to be maintained and distributed separately.** Companies such as W4 charge for that work.

For **PC and mobile**, Godot’s platform integration can be developed publicly and included in the free engine. Donations and community contributions fund that work, so you do not pay a separate exporter fee.

For **consoles**:

- **Access is restricted.** Developers need approval, confidential development tools and appropriate hardware. Console export templates cannot simply be posted for everyone to download.
- **Fewer people can contribute.** Contributors need the relevant access and equipment, making development and maintenance more expensive.
- **Maintenance continues after the initial port.** Someone must keep it working as Godot, console SDKs and hardware change.
- **Godot’s Foundation chooses to fund openly available work.** It does not want to divert substantial resources into ports that most contributors and users cannot access. Commercial providers take on that work instead.

Godot explains these reasons in its [console-porting explanation](https://godotengine.org/article/about-official-console-ports/) and [console guide](https://godotengine.org/consoles/).

**A paid exporter is not an unavoidable technical requirement.** RAWRLAB’s no-access-fee Nintendo port demonstrates that a restricted-access community port can exist. You can also build your own after obtaining platform access—but then you supply the engineering effort.

Also, **free mobile export does not mean free store distribution**. For example, Apple distinguishes free personal-device testing from developer-program membership for distribution. Those platform costs are separate from Godot’s exporter. [Apple developer accounts](https://developer.apple.com/help/account/basics/about-your-developer-account)
