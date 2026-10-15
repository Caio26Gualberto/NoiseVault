using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Archive;
using NoiseVault.Application.Events;
using NoiseVault.Application.Media;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Archive;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Ensaio;
using NoiseVault.Domain.Enums.Events;
using NoiseVault.Domain.Enums.Media;
using NoiseVault.Domain.Enums.Notifications;
using NoiseVault.Domain.Enums.Profiles;
using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Infra.Data.Context.Seeding
{
    public partial class DevSeed
    {
        private readonly List<(EntityBase Entity, DateTime CreatedAt, Func<Task> Apply)> _backdates = [];

        private async Task CreateContentAsync(Dictionary<string, User> people, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var admin = people["admin"];
            var lucia = people["lucia.ferraz"];
            var rafa = people["rafa.tavares"];
            var marina = people["marina.k"];
            var denis = people["denis.moura"];
            var paty = people["paty.lins"];
            var jorge = people["jorge.okamoto"];
            var bia = people["bia.santana"];
            var tiago = people["tiago.nunes"];

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var ferrugem = NewBand("Ferrugem", "ferrugem", SantoAndre, 1998, true,
                "Thrash old school do ABC desde 1998. Três demos em fita, um disco e muitas noites no Sacolão. Voltamos a ensaiar em 2025 com a formação original.",
                [EMusicGenre.Thrash, EMusicGenre.Heavy, EMusicGenre.Speed],
                [(admin, "Guitarra", EBandMemberRole.Owner, now.AddYears(-27)), (lucia, "Vocal", EBandMemberRole.Member, now.AddYears(-27)), (denis, "Baixo", EBandMemberRole.Member, now.AddYears(-3)), (marina, "Bateria", EBandMemberRole.Member, now.AddMonths(-8))],
                "https://instagram.com/ferrugem.abc", "https://youtube.com/@ferrugemthrash");

            var pogo = NewBand("Pogo Mortal", "pogo-mortal", SaoBernardo, 2015, false,
                "Punk e hardcore de São Bernardo. Músicas de um minuto e meio, letras sobre o ônibus que não passa.",
                [EMusicGenre.Punk, EMusicGenre.Hardcore, EMusicGenre.Crossover],
                [(marina, "Vocal e guitarra", EBandMemberRole.Owner, now.AddYears(-10)), (tiago, "Bateria", EBandMemberRole.Member, now.AddYears(-4)), (jorge, "Baixo", EBandMemberRole.Member, now.AddYears(-1))],
                "https://instagram.com/pogomortal", null);

            var vulto = NewBand("Vulto", "vulto", Diadema, 2021, false,
                "Doom e sludge de Diadema. Andamento lento, amplificador no talo, luz apagada.",
                [EMusicGenre.Doom, EMusicGenre.Sludge, EMusicGenre.Stoner],
                [(denis, "Baixo e voz", EBandMemberRole.Owner, now.AddYears(-4)), (bia, "Teclado", EBandMemberRole.Member, now.AddYears(-1))],
                null, "https://youtube.com/@vultodoom");

            var cinzas = NewBand("Cinzas", "cinzas", SaoCaetano, 2009, true,
                "Death metal melódico de São Caetano. Dois EPs e um split com banda de Curitiba.",
                [EMusicGenre.Death, EMusicGenre.MelodicDeath],
                [(rafa, "Guitarra", EBandMemberRole.Owner, now.AddYears(-16)), (tiago, "Bateria", EBandMemberRole.Member, now.AddYears(-6))],
                "https://instagram.com/cinzasdeath", null);

            _context.Bands.AddRange(ferrugem, pogo, vulto, cinzas);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var band in new[] { ferrugem, pogo, vulto, cinzas })
            {
                band.AvatarKey = DevSeedMedia.ImageKey($"bands/{band.Id}/avatar", "avatar");
                band.BannerKey = DevSeedMedia.ImageKey($"bands/{band.Id}/banner", "banner");
            }

            var inviteJorge = new BandInvitation { BandId = ferrugem.Id, InvitedUserId = jorge.Id, InvitedByUserId = admin.Id, Instrument = "Segunda guitarra", Status = EBandInvitationStatus.Pending, ExpiresAt = now.AddDays(12) };
            var inviteBia = new BandInvitation { BandId = cinzas.Id, InvitedUserId = bia.Id, InvitedByUserId = rafa.Id, Instrument = "Teclado", Status = EBandInvitationStatus.Pending, ExpiresAt = now.AddDays(9) };
            var invitePaty = new BandInvitation { BandId = pogo.Id, InvitedUserId = paty.Id, InvitedByUserId = marina.Id, Instrument = "Fotos e merch", Status = EBandInvitationStatus.Declined, ExpiresAt = now.AddDays(-2), RespondedAt = now.AddDays(-10) };
            _context.BandInvitations.AddRange(inviteJorge, inviteBia, invitePaty);
            await _context.SaveChangesAsync(cancellationToken);

            var showPhotos = new[] { NewPhoto(admin, "show-1"), NewPhoto(admin, "show-2"), NewPhoto(admin, "show-3") };
            var rehearsalVideo = NewVideo(admin, "ensaio-lamina-fria");
            var flyerPhoto = NewPhoto(marina, "flyer-pogo");
            var backstagePhoto = NewPhoto(rafa, "backstage");
            var pitPhotos = new[] { NewPhoto(paty, "pista-1"), NewPhoto(paty, "pista-2") };
            var archive1999 = new[] { NewPhoto(admin, "archive-1999-1"), NewPhoto(admin, "archive-1999-2"), NewPhoto(admin, "archive-1999-3") };
            var archive1999Video = NewVideo(admin, "archive-1999-vhs", EMediaProfile.Archive);
            var archive2003 = new[] { NewPhoto(admin, "archive-2003-1"), NewPhoto(admin, "archive-2003-2") };
            var archive2016 = new[] { NewPhoto(admin, "archive-2016-1"), NewPhoto(admin, "archive-2016-2") };
            var archive1980 = new[] { NewPhoto(admin, "archive-1980-ozzy") };
            var submissionPhoto = NewPhoto(paty, "archive-2008-galpao");
            var rejectedPhoto = NewPhoto(jorge, "archive-2011-praca");
            var supportPhoto = NewPhoto(paty, "support-foto-branca", EMediaProfile.Support);

            _context.MediaUploads.AddRange(showPhotos);
            _context.MediaUploads.AddRange(rehearsalVideo, flyerPhoto, backstagePhoto, archive1999Video, submissionPhoto, rejectedPhoto, supportPhoto);
            _context.MediaUploads.AddRange(archive1980);
            _context.MediaUploads.AddRange(pitPhotos);
            _context.MediaUploads.AddRange(archive1999);
            _context.MediaUploads.AddRange(archive2003);
            _context.MediaUploads.AddRange(archive2016);
            await _context.SaveChangesAsync(cancellationToken);

            var postRehearsalTalk = NewPost(lucia, null, "Ensaio da @ferrugem ontem foi pesado. Voltamos com a \"Lâmina Fria\" no set depois de quinze anos, pra abrir pro Necromancia no Santo Rock Bar. O @admin ainda lembra todos os solos.", now.AddHours(-20));
            var postShow = NewPost(admin, null, "Registro do show de sábado no Santo Rock Bar. Valeu quem foi, a pista não parou um minuto. @marina.k @lucia.ferraz", now.AddDays(-2), showPhotos);
            var postRehearsalVideo = NewPost(admin, ferrugem, "Trecho de \"Lâmina Fria\" no Santo Rock Bar, sábado. Som da mesa, sem mixagem, do jeito que saiu.", now.AddDays(-1), rehearsalVideo);
            var postFlyer = NewPost(marina, null, "Flyer saiu! Pogo na Praça no fim do mês, entrada franca. Traz a sua tribo, o som é de todo mundo.", now.AddDays(-3), flyerPhoto);
            var postBackstage = NewPost(rafa, null, "Palco armado e a galera já na frente antes do primeiro acorde. Marquei quem subiu comigo, quem faltar se acusa.", now.AddDays(-4), backstagePhoto);
            var postPit = NewPost(paty, null, "Arquivo pessoal: duas da pista no Vulto ao vivo. @denis.moura olha o grave balançando a lente.", now.AddDays(-6), pitPhotos);
            var postRoom = NewPost(denis, null, "Procuro sala de ensaio em Diadema que aceite doom às 23h. Dicas?", now.AddHours(-5));
            var postBlast = NewPost(tiago, null, "Blast beat a 240 bpm e o vizinho ainda não reclamou. Recorde da casa.", now.AddDays(-8));
            var postTape = NewPost(jorge, null, "Alguém tem a fita do MX no Teatro Lauro Gomes, começo dos anos 90? Perdi a minha na mudança e aquele \"Mental Slavery\" ao vivo não existe em lugar nenhum.", now.AddDays(-5));

            _context.Posts.AddRange(postRehearsalTalk, postShow, postRehearsalVideo, postFlyer, postBackstage, postPit, postRoom, postBlast, postTape);
            await _context.SaveChangesAsync(cancellationToken);

            AddLikes(postShow, lucia, marina, paty, rafa, jorge, bia);
            AddLikes(postRehearsalVideo, lucia, denis, marina, tiago);
            AddLikes(postFlyer, tiago, paty, admin);
            AddLikes(postBackstage, lucia, marina, admin);
            AddLikes(postPit, denis, bia);
            AddLikes(postRehearsalTalk, admin, denis);
            AddLikes(postBlast, marina, rafa);
            AddLikes(postTape, admin, lucia, denis);

            AddComment(postShow, paty, "Fotos minhas? Haha, manda os créditos!", now.AddDays(-2).AddHours(1));
            AddComment(postShow, marina, "Que som! A pista virou um liquidificador no bis.", now.AddDays(-2).AddHours(2));
            AddComment(postShow, admin, "@paty.lins claro, as melhores são suas. Crédito no mural também.", now.AddDays(-2).AddHours(3));
            AddComment(postRoom, tiago, "A sala do Nando, atrás do terminal. Ele fecha às 2h e não reclama de volume.", now.AddHours(-4));
            AddComment(postRoom, jorge, "Em Ribeirão tem uma que fecha à meia-noite, se servir.", now.AddHours(-3));
            AddComment(postRehearsalVideo, lucia, "Faltou o grito do final, foi cortado!", now.AddHours(-20));
            AddComment(postRehearsalTalk, admin, "@lucia.ferraz os solos lembram, o dedo é que já não acompanha.", now.AddHours(-18));
            AddComment(postTape, admin, "Tenho uma cópia da cópia, chiada. Publico no mural das antigas essa semana.", now.AddDays(-5).AddHours(2));

            _context.PhotoTags.AddRange(
                new PhotoTag { MediaUploadId = backstagePhoto.Id, X = 0.3, Y = 0.47, TaggedUserId = lucia.Id, TaggedByUserId = rafa.Id },
                new PhotoTag { MediaUploadId = backstagePhoto.Id, X = 0.46, Y = 0.46, TaggedUserId = marina.Id, TaggedByUserId = rafa.Id },
                new PhotoTag { MediaUploadId = backstagePhoto.Id, X = 0.55, Y = 0.5, Name = "Carlão da Mortalha", TaggedByUserId = rafa.Id },
                new PhotoTag { MediaUploadId = showPhotos[0].Id, X = 0.5, Y = 0.35, TaggedUserId = lucia.Id, TaggedByUserId = admin.Id });
            await _context.SaveChangesAsync(cancellationToken);

            var noiteDoAco = NewEvent(admin, ferrugem, "Noite do Aço: Necromancia, Ferrugem e Vulto", now.AddDays(12).Date.AddHours(24), "Santo Rock Bar", SantoAndre, 30m, 300,
                "Necromancia volta ao ABC depois de dez anos, com Ferrugem e Vulto abrindo. Abertura às 21h, primeira banda 21h30. Ingresso na porta, meia pra quem levar 1 kg de alimento.",
                [EMusicGenre.Thrash, EMusicGenre.Heavy, EMusicGenre.Doom], EEventStatus.Published, now.AddDays(-7),
                [(null, "Necromancia"), (ferrugem, null), (vulto, null)]);

            var mxNoSanto = NewEvent(rafa, cinzas, "MX e Cinzas no Santo Rock Bar", now.AddDays(33).Date.AddHours(23), "Santo Rock Bar", SantoAndre, 40m, 250,
                "MX tocando o \"Simoniacal\" na íntegra, com a Cinzas abrindo. Casa pequena, ingresso antecipado na bilheteria do bar.",
                [EMusicGenre.Thrash, EMusicGenre.Death], EEventStatus.Published, now.AddDays(-2),
                [(null, "MX"), (cinzas, null)]);

            var pogoNaPraca = NewEvent(marina, pogo, "Pogo na Praça", now.AddDays(25).Date.AddHours(19), "Praça da Matriz", SaoBernardo, null, null,
                "Edição de rua, entrada franca, som na caixa da prefeitura. Pogo Mortal, Cinzas, Os Sem Teto e Calçada Suja. Leva garrafa de água.",
                [EMusicGenre.Punk, EMusicGenre.Hardcore], EEventStatus.Published, now.AddDays(-3),
                [(pogo, null), (cinzas, null), (null, "Os Sem Teto"), (null, "Calçada Suja")]);

            var vultoPorao = NewEvent(denis, vulto, "Vulto ao vivo no Teatro Clara Nunes", now.AddDays(-20).Date.AddHours(23), "Teatro Clara Nunes", Diadema, 15m, 80,
                "Set completo do primeiro disco e uma inédita, com a casa no escuro. Névoa abre.",
                [EMusicGenre.Doom, EMusicGenre.Sludge], EEventStatus.Published, now.AddDays(-35),
                [(vulto, null), (null, "Névoa")]);

            var metalNaSerra = NewEvent(jorge, null, "Metal na Serra", now.AddDays(40).Date.AddHours(20), "Centro Cultural de Ribeirão Pires", RibeiraoPires, 20m, 200,
                "Festival cancelado pela produção por falta de alvará. MX, Ferrugem e Cinzas estavam confirmadas; o ingresso será devolvido.",
                [EMusicGenre.Heavy, EMusicGenre.Thrash], EEventStatus.Cancelled, now.AddDays(-12),
                [(null, "MX"), (ferrugem, null), (cinzas, null)]);
            metalNaSerra.CancelledAt = now.AddDays(-1);

            var tardeHardcore = NewEvent(paty, null, "Tarde de Hardcore em Mauá", now.AddDays(5).Date.AddHours(17), "Quadra do Jardim Zaíra", Maua, null, null,
                "Tarde de show na quadra, de graça, com a Pogo Mortal e a Reação. Vai ter troca de fita e zine na mesa da entrada.",
                [EMusicGenre.Hardcore, EMusicGenre.Punk], EEventStatus.Published, now.AddDays(-4),
                [(pogo, null), (null, "Reação")]);

            _context.Events.AddRange(noiteDoAco, mxNoSanto, pogoNaPraca, vultoPorao, metalNaSerra, tardeHardcore);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var item in new[] { noiteDoAco, mxNoSanto, pogoNaPraca, vultoPorao, tardeHardcore })
                item.BannerKey = DevSeedMedia.ImageKey($"events/{item.Id}/banner", "banner");

            AddInterests(noiteDoAco, lucia, marina, paty, jorge, bia, tiago, rafa);
            AddInterests(mxNoSanto, admin, lucia, jorge, tiago, denis, paty);
            AddInterests(pogoNaPraca, tiago, paty, rafa, admin, jorge);
            AddInterests(vultoPorao, bia, paty, admin);
            AddInterests(metalNaSerra, admin, rafa);
            AddInterests(tardeHardcore, marina, tiago, jorge, admin, lucia);

            AddEventComment(noiteDoAco, paty, "Vou de câmera. Alguém me guarda um lugar na grade?", now.AddDays(-3));
            AddEventComment(noiteDoAco, jorge, "Que horas abre a casa?", now.AddDays(-2));
            AddEventComment(noiteDoAco, admin, "@jorge.okamoto 21h, primeira banda 21h30. Chega cedo que o Santo Rock enche.", now.AddDays(-2).AddHours(1));
            AddEventComment(mxNoSanto, jorge, "Simoniacal inteiro? Vou com a camiseta de 89.", now.AddDays(-1));
            AddEventComment(vultoPorao, paty, "Fotos já estão no feed!", now.AddDays(-19));
            AddEventComment(pogoNaPraca, tiago, "Vai ter tomada pra pedal ou é tudo na caixa?", now.AddDays(-1));

            foreach (var item in new[] { noiteDoAco, mxNoSanto, pogoNaPraca, vultoPorao, metalNaSerra, tardeHardcore })
                _context.Posts.Add(Backdate(new Post { AuthorUserId = item.OrganizerUserId, BandId = item.BandId, EventId = item.Id, Content = string.Empty, PublishedAt = SeedCreatedAt(item) }, SeedCreatedAt(item)));

            await _context.SaveChangesAsync(cancellationToken);

            var entry1999 = NewArchive(admin, "Necromancia e Ferrugem no Teatro Municipal", 1999, 7, 17, "Teatro Municipal de Santo André", SantoAndre,
                "Primeira vez que thrash tocou no Teatro Municipal: Necromancia fechando e a Ferrugem abrindo com três anos de banda. O som estourou na segunda música e o técnico quase desligou tudo. Fotos do Beto, que não está mais entre nós, e uma fita VHS gravada da plateia.",
                [(null, "Necromancia"), (ferrugem, null)], archive1999, archive1999Video, now.AddDays(-15));
            var entry1980 = NewArchive(admin, "Ozzy Osbourne em Cardiff, turnê do Blizzard of Ozz", 1980, 9, null, "Sophia Gardens", null,
                "O Jorge morou fora em 80 e voltou com essa foto na mala: Ozzy recém-saído do Sabbath, Randy Rhoads na guitarra. Foi a foto que rodou de mão em mão nos ensaios do ABC a década inteira.",
                [(null, "Ozzy Osbourne")], archive1980, null, now.AddDays(-11), cityName: "Cardiff, País de Gales");
            var entry2003 = NewArchive(admin, "Korzus e Ferrugem no Teatro Lauro Gomes", 2003, 8, 23, "Teatro Lauro Gomes", SaoBernardo,
                "Korzus fechando a noite e a Ferrugem abrindo, com o teatro lotado e cadeira arrancada do lugar. O Marcello Pompeu desceu pra pista no bis.",
                [(null, "Korzus"), (ferrugem, null)], archive2003, null, now.AddDays(-9));
            var entry2016 = NewArchive(admin, "Pogo na Praça, a primeira edição", 2016, 11, null, "Praça da Matriz", SaoBernardo,
                "Como tudo começou: um gerador emprestado, duas caixas e a Pogo Mortal tocando até a polícia chegar.",
                [(pogo, null)], archive2016, null, now.AddDays(-5));
            var entryPending = NewArchive(paty, "Galpão da Vila Luzita, 2008", 2008, 3, 8, "Galpão da Vila Luzita", SantoAndre,
                "Festival de um dia só, com a Ferrugem fechando. Fotografei com uma compacta emprestada.",
                [(ferrugem, null), (null, "Sangue Ruim")], [submissionPhoto], null, now.AddDays(-1), submission: true, status: EArchiveEntryStatus.Pending);
            var entryRejected = NewArchive(jorge, "Show na praça, 2011", 2011, 5, null, "Praça do Paço", SantoAndre,
                "Acho que foi em 2011, ou 2012. A banda era a Mortalha.",
                [(null, "Mortalha")], [rejectedPhoto], null, now.AddDays(-4), submission: true, status: EArchiveEntryStatus.Rejected);
            entryRejected.ReviewedByUserId = admin.Id;
            entryRejected.ReviewedAt = now.AddDays(-2);
            entryRejected.RejectionReason = "A Mortalha confirmou que esse show foi em 2013, no Sacolão. Confere a data e o lugar e manda de novo.";

            _context.ArchiveEntries.AddRange(entry1999, entry1980, entry2003, entry2016, entryPending, entryRejected);
            await _context.SaveChangesAsync(cancellationToken);

            AddAttendances(entry1999, lucia, jorge, denis, admin);
            AddAttendances(entry1980, jorge);
            AddArchiveLikes(entry1980, lucia, rafa, tiago, admin);
            AddAttendances(entry2016, marina, tiago, paty);
            AddArchiveLikes(entry1999, lucia, jorge, denis, marina, paty, rafa, bia);
            AddArchiveLikes(entry2003, rafa, tiago, jorge, lucia);
            AddAttendances(entry2003, lucia, admin, jorge);
            AddArchiveLikes(entry2016, tiago, paty, admin);

            AddArchiveComment(entry1999, lucia, "Eu tava no palco e nem lembrava dessa foto. Olha o cabelo.", now.AddDays(-14));
            AddArchiveComment(entry1999, jorge, "Primeiro show que fui com meu irmão. A fita VHS é dele!", now.AddDays(-13));
            AddArchiveComment(entry2016, marina, "O gerador era do pai do Tiago. Nunca devolvemos.", now.AddDays(-4));
            AddArchiveComment(entry1980, jorge, "Vinte libras e eu achando caro. Hoje pagaria dez vezes isso.", now.AddDays(-10));

            foreach (var entry in new[] { entry1999, entry1980, entry2003, entry2016 })
                _context.Posts.Add(Backdate(new Post { AuthorUserId = entry.AuthorUserId, ArchiveEntryId = entry.Id, Content = string.Empty, PublishedAt = SeedCreatedAt(entry) }, SeedCreatedAt(entry)));

            await _context.SaveChangesAsync(cancellationToken);

            var musicianJorge = NewMusician(jorge, true, now.AddDays(-10), EMusicianGoal.Either, EMusicianCommitment.PlayLive, 25, true, true, false,
                "Iron Maiden, Thin Lizzy, Angra, Judas Priest",
                [(EInstrument.Guitar, ESkillLevel.Advanced), (EInstrument.Bass, ESkillLevel.Intermediate)],
                [EMusicGenre.Heavy, EMusicGenre.HardRock, EMusicGenre.ClassicRock],
                [(EWeekDay.Tuesday, EDayPeriod.Night), (EWeekDay.Thursday, EDayPeriod.Night), (EWeekDay.Saturday, EDayPeriod.Afternoon)],
                ["https://youtube.com/watch?v=jorge-solo-ao-vivo"]);
            var musicianBia = NewMusician(bia, true, now.AddDays(-3), EMusicianGoal.Original, EMusicianCommitment.Record, 15, true, false, false,
                "Type O Negative, Paradise Lost, Nightwish",
                [(EInstrument.Keys, ESkillLevel.Advanced), (EInstrument.Vocal, ESkillLevel.Intermediate)],
                [EMusicGenre.Gothic, EMusicGenre.Symphonic, EMusicGenre.Doom],
                [(EWeekDay.Wednesday, EDayPeriod.Night), (EWeekDay.Sunday, EDayPeriod.Afternoon)],
                ["https://soundcloud.com/biasantana/noite-sem-fim"]);
            var musicianTiago = NewMusician(tiago, true, now.AddDays(-30), EMusicianGoal.Original, EMusicianCommitment.PlayLive, 40, true, false, true,
                "Napalm Death, Converge, Nasum",
                [(EInstrument.Drums, ESkillLevel.Advanced)],
                [EMusicGenre.Grindcore, EMusicGenre.Deathcore, EMusicGenre.Hardcore],
                [(EWeekDay.Friday, EDayPeriod.Night), (EWeekDay.Saturday, EDayPeriod.Night)],
                []);
            var musicianRafa = NewMusician(rafa, true, now.AddDays(-1), EMusicianGoal.Original, EMusicianCommitment.Record, 30, true, true, false,
                "At the Gates, Insomnium, Dark Tranquillity",
                [(EInstrument.Guitar, ESkillLevel.Advanced)],
                [EMusicGenre.Death, EMusicGenre.MelodicDeath],
                [(EWeekDay.Monday, EDayPeriod.Night), (EWeekDay.Saturday, EDayPeriod.Morning)],
                ["https://youtube.com/watch?v=cinzas-ep-2"]);
            var musicianLucia = NewMusician(lucia, false, null, EMusicianGoal.Covers, EMusicianCommitment.JustRehearse, null, false, true, false,
                "Doro, Sepultura, Exodus",
                [(EInstrument.Vocal, ESkillLevel.Advanced)],
                [EMusicGenre.Thrash, EMusicGenre.Heavy],
                [(EWeekDay.Tuesday, EDayPeriod.Night)],
                []);

            _context.MusicianProfiles.AddRange(musicianJorge, musicianBia, musicianTiago, musicianRafa, musicianLucia);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var profile in new[] { musicianJorge, musicianBia, musicianRafa })
                profile.PhotoKey = DevSeedMedia.ImageKey($"users/{profile.UserId}/musician", "flyer");

            var openingFerrugem = NewOpening(ferrugem, EInstrument.Guitar, "Segunda guitarra para shows e gravação do próximo disco. Thrash old school, ensaio terça à noite em Santo André. Equipamento próprio ajuda.", EMusicianGoal.Original, EBandOpeningStatus.Open, now.AddDays(45), now.AddDays(-15));
            var openingPogo = NewOpening(pogo, EInstrument.Bass, "Baixista pra fechar a formação. Punk e hardcore, três ensaios por mês, shows no ABC e na capital.", EMusicianGoal.Either, EBandOpeningStatus.Open, now.AddDays(30), now.AddDays(-30));
            var openingVulto = NewOpening(vulto, EInstrument.Drums, "Baterista que curta andamento lento e peso. Sem pressa, com volume.", EMusicianGoal.Original, EBandOpeningStatus.Filled, now.AddDays(10), now.AddDays(-50));
            openingVulto.ClosedAt = now.AddDays(-3);
            var openingCinzas = NewOpening(cinzas, EInstrument.Vocal, "Vocal gutural e rasgado pra set de death melódico. Gravação em outubro, shows em seguida.", EMusicianGoal.Original, EBandOpeningStatus.Open, now.AddDays(50), now.AddDays(-10));

            _context.BandOpenings.AddRange(openingFerrugem, openingPogo, openingVulto, openingCinzas);
            await _context.SaveChangesAsync(cancellationToken);

            _context.BandOpeningInterests.AddRange(
                new BandOpeningInterest { BandOpeningId = openingFerrugem.Id, UserId = jorge.Id, Message = "Toco há vinte anos, tenho equipamento e carro. Terça à noite funciona pra mim.", Status = EOpeningInterestStatus.Pending },
                new BandOpeningInterest { BandOpeningId = openingPogo.Id, UserId = denis.Id, Message = "Sou de doom, mas já toquei punk. Posso cobrir até acharem alguém fixo.", Status = EOpeningInterestStatus.Dismissed },
                new BandOpeningInterest { BandOpeningId = openingCinzas.Id, UserId = lucia.Id, Message = "Faço gutural desde o Ferrugem acústico que nunca saiu. Manda um trecho?", Status = EOpeningInterestStatus.Pending });

            foreach (var (opening, owner) in new[] { (openingFerrugem, admin), (openingPogo, marina), (openingVulto, denis), (openingCinzas, rafa) })
                _context.Posts.Add(Backdate(new Post { AuthorUserId = owner.Id, BandId = opening.BandId, BandOpeningId = opening.Id, Content = string.Empty, PublishedAt = SeedCreatedAt(opening) }, SeedCreatedAt(opening)));

            await _context.SaveChangesAsync(cancellationToken);

            var ticketPhoto = NewTicket(paty, "Foto não abre no celular", "Quando toco na foto do post do admin no celular, ela abre em branco e fica só o fundo escuro. No computador funciona normal. Android 14, Chrome atualizado.",
                ESupportTicketStatus.Open, ESupportTicketPriority.Normal, [ESupportTag.Bug, ESupportTag.Mobile], [ESupportArea.Feed], "/feed",
                "Mozilla/5.0 (Linux; Android 14; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Mobile Safari/537.36", 412, 915, now.AddDays(-1), null, [supportPhoto]);
            var ticketSuggestion = NewTicket(jorge, "Sugestão: filtro de instrumento com cidade", "Seria bom filtrar a vitrine do Ensaio por instrumento e por cidade ao mesmo tempo. Hoje escolho um e perco o outro, e acabo rolando a lista inteira.",
                ESupportTicketStatus.InProgress, ESupportTicketPriority.Low, [ESupportTag.Suggestion, ESupportTag.Desktop], [ESupportArea.Other], "/ensaio",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36", 1920, 1080, now.AddHours(-3), null, []);
            var ticketResolved = NewTicket(tiago, "Não consigo sair da banda", "O botão de sair da banda não aparece no celular. No computador consegui, mas queria avisar que no celular ele some.",
                ESupportTicketStatus.Resolved, ESupportTicketPriority.High, [ESupportTag.Bug, ESupportTag.Mobile], [ESupportArea.Bands], "/bandas/cinzas",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1", 390, 844, now.AddDays(-5), now.AddDays(-5), []);
            ticketResolved.InternalNote = "Botão ficava atrás da barra inferior no iOS. Corrigido no front.";

            _context.SupportTickets.AddRange(ticketPhoto, ticketSuggestion, ticketResolved);
            await _context.SaveChangesAsync(cancellationToken);

            AddTicketMessage(ticketSuggestion, admin, true, "Boa! Já está no backlog e deve sair na próxima leva do Ensaio.", now.AddHours(-4));
            AddTicketMessage(ticketSuggestion, jorge, false, "Valeu, fico no aguardo.", now.AddHours(-3));
            AddTicketMessage(ticketResolved, admin, true, "Corrigido ontem à noite. Pode tentar de novo e me avisa se continuar.", now.AddDays(-6));
            AddTicketMessage(ticketResolved, tiago, false, "Funcionou, obrigado!", now.AddDays(-5));

            _context.Notifications.AddRange(
                NewNotification(admin, ENotificationKind.PostLiked, lucia, now.AddDays(-2).AddHours(4), postId: postShow.Id, actorCount: 6),
                NewNotification(admin, ENotificationKind.PostCommented, paty, now.AddDays(-2).AddHours(1), postId: postShow.Id, actorCount: 2),
                NewNotification(admin, ENotificationKind.MentionedInPost, lucia, now.AddHours(-20), postId: postRehearsalTalk.Id, readAt: now.AddHours(-19)),
                NewNotification(admin, ENotificationKind.EventInterest, marina, now.AddDays(-3), eventId: noiteDoAco.Id, actorCount: 7),
                NewNotification(admin, ENotificationKind.EventCommented, jorge, now.AddDays(-2), eventId: noiteDoAco.Id),
                NewNotification(admin, ENotificationKind.ArchiveAttended, jorge, now.AddDays(-13), archiveEntryId: entry1999.Id, actorCount: 3, readAt: now.AddDays(-12)),
                NewNotification(admin, ENotificationKind.ArchiveCommented, lucia, now.AddDays(-14), archiveEntryId: entry1999.Id, actorCount: 2, readAt: now.AddDays(-12)),
                NewNotification(admin, ENotificationKind.BandOpeningInterest, jorge, now.AddHours(-30), bandId: ferrugem.Id, bandOpeningId: openingFerrugem.Id),
                NewNotification(admin, ENotificationKind.ArchiveSubmissionReceived, paty, now.AddDays(-1), archiveEntryId: entryPending.Id),
                NewNotification(admin, ENotificationKind.SupportTicketCreated, paty, now.AddDays(-1), supportTicketId: ticketPhoto.Id),
                NewNotification(admin, ENotificationKind.PostCommented, lucia, now.AddHours(-20), postId: postRehearsalVideo.Id),
                NewNotification(jorge, ENotificationKind.BandInvite, admin, now.AddDays(-2), bandId: ferrugem.Id, bandInvitationId: inviteJorge.Id),
                NewNotification(jorge, ENotificationKind.ArchiveSubmissionRejected, admin, now.AddDays(-2), archiveEntryId: entryRejected.Id),
                NewNotification(jorge, ENotificationKind.SupportTicketReplied, admin, now.AddHours(-4), supportTicketId: ticketSuggestion.Id),
                NewNotification(lucia, ENotificationKind.PhotoTagged, rafa, now.AddDays(-4), postId: postBackstage.Id),
                NewNotification(marina, ENotificationKind.PhotoTagged, rafa, now.AddDays(-4), postId: postBackstage.Id),
                NewNotification(marina, ENotificationKind.MentionedInPost, admin, now.AddDays(-2), postId: postShow.Id),
                NewNotification(denis, ENotificationKind.MentionedInPost, paty, now.AddDays(-6), postId: postPit.Id),
                NewNotification(denis, ENotificationKind.PostCommented, tiago, now.AddHours(-4), postId: postRoom.Id, actorCount: 2),
                NewNotification(bia, ENotificationKind.BandInvite, rafa, now.AddDays(-5), bandId: cinzas.Id, bandInvitationId: inviteBia.Id),
                NewNotification(paty, ENotificationKind.PostCommented, admin, now.AddDays(-2).AddHours(3), postId: postShow.Id),
                NewNotification(tiago, ENotificationKind.SupportTicketStatusChanged, admin, now.AddDays(-5), supportTicketId: ticketResolved.Id, readAt: now.AddDays(-5)));

            await _context.SaveChangesAsync(cancellationToken);

            foreach (var (_, _, apply) in _backdates)
                await apply();

            _backdates.Clear();

            await transaction.CommitAsync(cancellationToken);
        }

        private static Band NewBand(string name, string slug, int cityId, int foundedYear, bool verified, string description, EMusicGenre[] genres,
            (User User, string Instrument, EBandMemberRole Role, DateTime JoinedAt)[] members, string? instagram, string? youtube) => new()
        {
            Name = name,
            Slug = slug,
            Description = description,
            CityId = cityId,
            FoundedYear = foundedYear,
            IsVerified = verified,
            InstagramUrl = instagram,
            YoutubeUrl = youtube,
            Genres = genres.Select(g => new BandGenre { Genre = g }).ToList(),
            Members = members.Select(m => new BandMember { UserId = m.User.Id, Instrument = m.Instrument, Role = m.Role, JoinedAt = m.JoinedAt }).ToList(),
        };

        private static MediaUpload NewPhoto(User owner, string name, EMediaProfile profile = EMediaProfile.Post)
        {
            var key = DevSeedMedia.ImageKey($"users/{owner.Id}/photos", name);
            var (width, height) = DevSeedMedia.ImageSize(key);

            return new MediaUpload
            {
                OwnerUserId = owner.Id,
                Kind = EMediaKind.Photo,
                Profile = name.StartsWith("archive-", StringComparison.Ordinal) ? EMediaProfile.Archive : profile,
                Status = EMediaStatus.Ready,
                StorageKey = key,
                Width = width,
                Height = height,
                SizeBytes = 0,
                ExpiresAt = DateTime.UtcNow.Add(MediaRules.UnattachedLifetime),
            };
        }

        private static MediaUpload NewVideo(User owner, string name, EMediaProfile profile = EMediaProfile.Post)
        {
            var folder = DevSeedMedia.VideoFolder(owner.Id, name);

            return new MediaUpload
            {
                OwnerUserId = owner.Id,
                Kind = EMediaKind.Video,
                Profile = profile,
                Status = EMediaStatus.Ready,
                StorageKey = DevSeedMedia.VideoKey(folder),
                PosterKey = DevSeedMedia.PosterKey(folder),
                Width = 1280,
                Height = 720,
                DurationSeconds = 6,
                SizeBytes = 0,
                ExpiresAt = DateTime.UtcNow.Add(MediaRules.UnattachedLifetime),
            };
        }

        private Post NewPost(User author, Band? band, string content, DateTime publishedAt, params MediaUpload[] media) =>
            Backdate(new Post
            {
                AuthorUserId = author.Id,
                BandId = band?.Id,
                Content = content,
                PublishedAt = publishedAt,
                Media = media.Select((m, index) => new PostMedia { MediaUploadId = m.Id, Order = index }).ToList(),
            }, publishedAt);

        private void AddLikes(Post post, params User[] users) =>
            _context.PostLikes.AddRange(users.Select((u, index) => new PostLike { PostId = post.Id, UserId = u.Id, CreatedAt = (post.PublishedAt ?? DateTime.UtcNow).AddMinutes(30 + index) }));

        private void AddComment(Post post, User author, string text, DateTime at) =>
            _context.Comments.Add(Backdate(new Comment { PostId = post.Id, AuthorUserId = author.Id, Text = text }, at));

        private Event NewEvent(User organizer, Band? band, string title, DateTime startsAt, string venue, int cityId, decimal? price, int? capacity, string description,
            EMusicGenre[] genres, EEventStatus status, DateTime createdAt, (Band? Band, string? Name)[] lineup) =>
            Backdate(new Event
            {
                OrganizerUserId = organizer.Id,
                BandId = band?.Id,
                Title = title,
                Description = description,
                StartsAt = startsAt,
                EndsAtOrDefault = EventRules.EndsAtOrDefault(startsAt, null),
                Venue = venue,
                CityId = cityId,
                Price = price,
                Capacity = capacity,
                Status = status,
                Genres = genres.Select(g => new EventGenre { Genre = g }).ToList(),
                Lineup = lineup.Select((l, index) => new EventLineupEntry { BandId = l.Band?.Id, Name = l.Name, Order = index }).ToList(),
            }, createdAt);

        private void AddInterests(Event item, params User[] users) =>
            _context.EventInterests.AddRange(users.Select((u, index) => new EventInterest { EventId = item.Id, UserId = u.Id, CreatedAt = SeedCreatedAt(item).AddHours(index + 1) }));

        private void AddEventComment(Event item, User author, string text, DateTime at) =>
            _context.EventComments.Add(Backdate(new EventComment { EventId = item.Id, AuthorUserId = author.Id, Text = text }, at));

        private ArchiveEntry NewArchive(User author, string title, int year, int? month, int? day, string? venue, int? cityId, string description,
            (Band? Band, string? Name)[] bands, MediaUpload[] photos, MediaUpload? video, DateTime createdAt, bool submission = false, EArchiveEntryStatus status = EArchiveEntryStatus.Published, string? cityName = null)
        {
            var media = photos.Concat(video == null ? [] : [video]).ToList();

            return Backdate(new ArchiveEntry
            {
                AuthorUserId = author.Id,
                Status = status,
                IsSubmission = submission,
                Title = title,
                Description = description,
                Year = year,
                Month = month,
                Day = day,
                SortDate = ArchiveRules.SortDate(year, month, day),
                Venue = venue,
                CityId = cityId,
                CityName = cityName,
                CoverMediaUploadId = photos[0].Id,
                Bands = bands.Select((b, index) => new ArchiveBandEntry { BandId = b.Band?.Id, Name = b.Name, Order = index }).ToList(),
                Media = media.Select((m, index) => new ArchiveMedia { MediaUploadId = m.Id, Order = index, Caption = index == 0 ? "Capa" : null }).ToList(),
            }, createdAt);
        }

        private void AddAttendances(ArchiveEntry entry, params User[] users) =>
            _context.ArchiveAttendances.AddRange(users.Select((u, index) => new ArchiveAttendance { ArchiveEntryId = entry.Id, UserId = u.Id, CreatedAt = SeedCreatedAt(entry).AddHours(index + 2) }));

        private void AddArchiveLikes(ArchiveEntry entry, params User[] users) =>
            _context.ArchiveLikes.AddRange(users.Select((u, index) => new ArchiveLike { ArchiveEntryId = entry.Id, UserId = u.Id, CreatedAt = SeedCreatedAt(entry).AddHours(index + 1) }));

        private void AddArchiveComment(ArchiveEntry entry, User author, string text, DateTime at) =>
            _context.ArchiveComments.Add(Backdate(new ArchiveComment { ArchiveEntryId = entry.Id, AuthorUserId = author.Id, Text = text }, at));

        private static MusicianProfile NewMusician(User user, bool looking, DateTime? lookingSince, EMusicianGoal goal, EMusicianCommitment commitment, int? reachKm,
            bool gear, bool transport, bool space, string influences, (EInstrument Instrument, ESkillLevel Level)[] instruments, EMusicGenre[] genres,
            (EWeekDay Day, EDayPeriod Period)[] availability, string[] links) => new()
        {
            UserId = user.Id,
            IsLookingForBand = looking,
            LookingSince = looking ? lookingSince : null,
            Goal = goal,
            Commitment = commitment,
            ReachKm = reachKm,
            HasGear = gear,
            HasTransport = transport,
            HasRehearsalSpace = space,
            Influences = influences,
            Instruments = instruments.Select((i, index) => new MusicianInstrument { Instrument = i.Instrument, Level = i.Level, Position = index }).ToList(),
            Genres = genres.Select(g => new MusicianGenre { Genre = g }).ToList(),
            Availability = availability.Select(a => new MusicianAvailability { Day = a.Day, Period = a.Period }).ToList(),
            SampleLinks = links.Select((l, index) => new MusicianSampleLink { Url = l, Position = index }).ToList(),
        };

        private BandOpening NewOpening(Band band, EInstrument instrument, string description, EMusicianGoal goal, EBandOpeningStatus status, DateTime expiresAt, DateTime createdAt) =>
            Backdate(new BandOpening
            {
                BandId = band.Id,
                Instrument = instrument,
                Description = description,
                Goal = goal,
                Status = status,
                ExpiresAt = expiresAt,
            }, createdAt);

        private SupportTicket NewTicket(User reporter, string title, string description, ESupportTicketStatus status, ESupportTicketPriority priority, ESupportTag[] tags, ESupportArea[] areas,
            string originPath, string userAgent, int viewportWidth, int viewportHeight, DateTime createdAt, DateTime? closedAt, MediaUpload[] media) =>
            Backdate(new SupportTicket
            {
                ReporterUserId = reporter.Id,
                Title = title,
                Description = description,
                Status = status,
                Priority = priority,
                OriginPath = originPath,
                UserAgent = userAgent,
                ViewportWidth = viewportWidth,
                ViewportHeight = viewportHeight,
                LastActivityAt = createdAt,
                ClosedAt = closedAt,
                Tags = tags.Select(t => new SupportTicketTag { Tag = t }).ToList(),
                Areas = areas.Select(a => new SupportTicketArea { Area = a }).ToList(),
                Media = media.Select((m, index) => new SupportTicketMedia { MediaUploadId = m.Id, Order = index }).ToList(),
            }, createdAt);

        private void AddTicketMessage(SupportTicket ticket, User author, bool fromStaff, string text, DateTime at) =>
            _context.SupportTicketMessages.Add(Backdate(new SupportTicketMessage { SupportTicketId = ticket.Id, AuthorUserId = author.Id, Text = text, FromStaff = fromStaff }, at));

        private static Notification NewNotification(User recipient, ENotificationKind kind, User actor, DateTime at, int? postId = null, int? eventId = null, int? archiveEntryId = null,
            int? bandId = null, int? bandInvitationId = null, int? supportTicketId = null, int? bandOpeningId = null, int actorCount = 1, DateTime? readAt = null) => new()
        {
            UserId = recipient.Id,
            Kind = kind,
            ActorUserId = actor.Id,
            ActorCount = actorCount,
            PostId = postId,
            EventId = eventId,
            ArchiveEntryId = archiveEntryId,
            BandId = bandId,
            BandInvitationId = bandInvitationId,
            SupportTicketId = supportTicketId,
            BandOpeningId = bandOpeningId,
            CreatedAt = at,
            LastActivityAt = at,
            ReadAt = readAt,
        };

        private DateTime SeedCreatedAt(EntityBase entity) => _backdates.First(b => ReferenceEquals(b.Entity, entity)).CreatedAt;

        private T Backdate<T>(T entity, DateTime createdAt) where T : EntityBase
        {
            _backdates.Add((entity, createdAt, async () =>
            {
                await _context.Set<T>()
                    .Where(e => e.Id == entity.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.CreatedAt, createdAt));

                _context.Entry(entity).State = EntityState.Detached;
            }));

            return entity;
        }
    }
}
