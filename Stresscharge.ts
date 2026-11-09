import * as flatbuffers from "flatbuffers";
import { randomUUID } from "node:crypto";
import { isIP } from "node:net";

type RequestType = "spawn" | "moveNPC" | "navmesh";

interface Options {
    host: string;
    port: number;
    rate: number;
    durationSeconds: number;
    type: RequestType;
    zone: string;
    uuid: string;
    start: [number, number, number];
    end: [number, number, number];
    drainSeconds: number;
}

const MAX_RATE = 50_000;
const MAX_DURATION_SECONDS = 300;
const MAX_CATCH_UP_BURST_MS = 50;

function usage(): string {
    return `Usage:
  bun run Stresscharge.ts [options]

Options:
  --host <IPv4>           Target server (default: 127.0.0.1; private IPv4 only)
  --port <number>         Target UDP port (default: 11000)
  --rate <number>         Target requests/second (default: 1000, max: ${MAX_RATE})
  --duration <seconds>    Load duration (default: 30, max: ${MAX_DURATION_SECONDS})
  --type <type>           spawn | moveNPC | navmesh (default: navmesh)
  --zone <name>           Zone string echoed by the server (default: stress-test)
  --uuid <id>             Request identifier (default: generated UUID)
  --start <x,y,z>         Start position (default: 0,0,0)
  --end <x,y,z>           End position (default: 10,0,10)
  --drain <seconds>       Continue collecting replies after sending (default: 2, max: 10)
  --help                  Show this help

Example:
  bun run Stresscharge.ts --host 192.168.1.61 --port 11000 --rate 1000 --duration 30 --type navmesh`;
}

function parseNumber(value: string | undefined, name: string): number {
    const parsed = Number(value);
    if (!Number.isFinite(parsed)) {
        throw new Error(`Valeur numérique invalide pour ${name}: ${value ?? "(manquante)"}`);
    }
    return parsed;
}

function parseVector(value: string | undefined, name: string, fallback: [number, number, number]): [number, number, number] {
    if (value === undefined) return fallback;

    const parts = value.split(",").map((part) => Number(part.trim()));
    if (parts.length !== 3 || parts.some((part) => !Number.isFinite(part))) {
        throw new Error(`${name} doit être au format x,y,z`);
    }

    return [parts[0], parts[1], parts[2]];
}

function isAllowedTarget(host: string): boolean {
    if (isIP(host) !== 4) return false;

    const [a, b] = host.split(".").map(Number);
    return a === 10
        || a === 127
        || (a === 172 && b >= 16 && b <= 31)
        || (a === 192 && b === 168);
}

function parseOptions(args: string[]): Options {
    const values = new Map<string, string>();
    const knownFlags = new Set([
        "--host", "--port", "--rate", "--duration", "--type",
        "--zone", "--uuid", "--start", "--end", "--drain",
    ]);

    for (let i = 0; i < args.length; i++) {
        const flag = args[i];
        if (flag === "--help") {
            console.log(usage());
            process.exit(0);
        }
        if (!knownFlags.has(flag)) throw new Error(`Option inconnue: ${flag}`);

        const value = args[++i];
        if (value === undefined || value.startsWith("--")) {
            throw new Error(`Valeur manquante pour ${flag}`);
        }
        values.set(flag, value);
    }

    const host = values.get("--host") ?? "127.0.0.1";
    if (!isAllowedTarget(host)) {
        throw new Error("La cible doit être une adresse IPv4 locale ou privée (127/8, 10/8, 172.16/12 ou 192.168/16).");
    }

    const port = parseNumber(values.get("--port") ?? "11000", "--port");
    const rate = parseNumber(values.get("--rate") ?? "1000", "--rate");
    const durationSeconds = parseNumber(values.get("--duration") ?? "30", "--duration");
    const drainSeconds = parseNumber(values.get("--drain") ?? "2", "--drain");
    const type = (values.get("--type") ?? "navmesh") as RequestType;

    if (!Number.isInteger(port) || port < 1 || port > 65_535) {
        throw new Error("--port doit être un entier entre 1 et 65535");
    }
    if (!Number.isInteger(rate) || rate < 1 || rate > MAX_RATE) {
        throw new Error(`--rate doit être un entier entre 1 et ${MAX_RATE}`);
    }
    if (!Number.isInteger(durationSeconds) || durationSeconds < 1 || durationSeconds > MAX_DURATION_SECONDS) {
        throw new Error(`--duration doit être un entier entre 1 et ${MAX_DURATION_SECONDS}`);
    }
    if (!Number.isFinite(drainSeconds) || drainSeconds < 0 || drainSeconds > 10) {
        throw new Error("--drain doit être compris entre 0 et 10 secondes");
    }
    if (type !== "spawn" && type !== "moveNPC" && type !== "navmesh") {
        throw new Error("--type doit être spawn, moveNPC ou navmesh");
    }

    return {
        host,
        port,
        rate,
        durationSeconds,
        drainSeconds,
        type,
        zone: values.get("--zone") ?? "stress-test",
        uuid: values.get("--uuid") ?? randomUUID(),
        start: parseVector(values.get("--start"), "--start", [0, 0, 0]),
        end: parseVector(values.get("--end"), "--end", [10, 0, 10]),
    };
}

function createVec3f(builder: flatbuffers.Builder, [x, y, z]: [number, number, number]): number {
    builder.startObject(3);
    builder.addFieldFloat32(2, z, 0);
    builder.addFieldFloat32(1, y, 0);
    builder.addFieldFloat32(0, x, 0);
    return builder.endObject();
}

function createRequest(options: Options): Uint8Array {
    const builder = new flatbuffers.Builder(256);
    const typeOffset = builder.createString(options.type);
    const zoneOffset = builder.createString(options.zone);
    const uuidOffset = builder.createString(options.uuid);
    const startOffset = createVec3f(builder, options.start);
    const endOffset = createVec3f(builder, options.end);

    builder.startVector(4, 0, 4);
    const pointsOffset = builder.endVector();

    builder.startObject(6);
    builder.addFieldOffset(5, pointsOffset, 0);
    builder.addFieldOffset(4, endOffset, 0);
    builder.addFieldOffset(3, startOffset, 0);
    builder.addFieldOffset(2, uuidOffset, 0);
    builder.addFieldOffset(1, zoneOffset, 0);
    builder.addFieldOffset(0, typeOffset, 0);
    const rootOffset = builder.endObject();
    builder.finish(rootOffset);

    return builder.asUint8Array();
}

async function main(): Promise<void> {
    let options: Options;
    try {
        options = parseOptions(Bun.argv.slice(2));
    } catch (error) {
        console.error(error instanceof Error ? error.message : error);
        console.error(usage());
        process.exitCode = 1;
        return;
    }

    const payload = createRequest(options);
    let sent = 0;
    let sendErrors = 0;
    let received = 0;
    let receivedBytes = 0;
    let socketErrors = 0;
    let lastSocketError = "";
    let lastSendError = "";
    let lastReportAt = performance.now();
    let lastReportSent = 0;
    let lastReportReceived = 0;

    const socket = await Bun.udpSocket({
        connect: {
            hostname: options.host,
            port: options.port,
        },
        socket: {
            data(_socket, data) {
                received++;
                receivedBytes += data.byteLength;
            },
            error(_socket, error) {
                socketErrors++;
                lastSocketError = error.message;
            },
        },
    });

    const sendUntil = performance.now() + options.durationSeconds * 1000;
    const sendStartedAt = performance.now();
    let attempted = 0;

    console.log(`Cible: ${options.host}:${options.port}`);
    console.log(`Charge: ${options.rate} req/s pendant ${options.durationSeconds}s, type=${options.type}, paquet=${payload.byteLength} octets`);
    console.log("Le test est limité à une cible locale/privée et à 5 minutes maximum.");

    try {
        while (performance.now() < sendUntil) {
            const now = performance.now();
            const targetAttempts = Math.floor((now - sendStartedAt) * options.rate / 1000) + 1;
            const due = targetAttempts - attempted;
            if (due <= 0) {
                await Bun.sleep(1);
                continue;
            }

            const maxBatchSize = Math.max(1, Math.ceil(options.rate * MAX_CATCH_UP_BURST_MS / 1000));
            const batchSize = Math.min(due, maxBatchSize);

            for (let i = 0; i < batchSize; i++) {
                attempted++;
                try {
                    const bytes = socket.send(payload);
                    if (bytes === payload.byteLength) sent++;
                    else sendErrors++;
                } catch (error) {
                    sendErrors++;
                    lastSendError = error instanceof Error ? error.message : String(error);
                }
            }

            const reportNow = performance.now();
            if (reportNow - lastReportAt >= 1000) {
                const seconds = (reportNow - lastReportAt) / 1000;
                const sentRate = (sent - lastReportSent) / seconds;
                const receivedRate = (received - lastReportReceived) / seconds;
                console.log(
                    `Envois acceptés: ${sentRate.toFixed(0)}/s | réponses reçues: ${receivedRate.toFixed(0)}/s | ` +
                    `total: ${sent} envoyés, ${received} reçus, ${sendErrors} erreurs d'envoi`,
                );
                lastReportAt = reportNow;
                lastReportSent = sent;
                lastReportReceived = received;
            }
        }

        if (options.drainSeconds > 0) {
            console.log(`Fin des envois. Collecte des réponses pendant ${options.drainSeconds}s...`);
            await Bun.sleep(options.drainSeconds * 1000);
        }
    } finally {
        socket.close();
    }

    const actualRate = sent / options.durationSeconds;
    const replyRate = received / options.durationSeconds;
    const replyRatio = sent === 0 ? 0 : received / sent * 100;
    console.log("\n--- Résultat ---");
    console.log(`Débit demandé       : ${options.rate} req/s`);
    console.log(`Débit d'envoi accepté: ${actualRate.toFixed(1)} datagrammes/s`);
    console.log(`Réponses reçues      : ${received} (${replyRate.toFixed(1)} réponses/s sur la durée d'envoi)`);
    console.log(`Ratio réponses/envois: ${replyRatio.toFixed(2)}% (approx.; UDP ne corrèle pas chaque réponse)`);
    console.log(`Octets envoyés       : ${sent * payload.byteLength}`);
    console.log(`Octets reçus         : ${receivedBytes}`);
    console.log(`Tentatives non faites: ${Math.max(0, Math.floor(options.rate * options.durationSeconds) - attempted)}`);
    console.log(`Erreurs d'envoi      : ${sendErrors}${lastSendError ? ` (dernière: ${lastSendError})` : ""}`);
    console.log(`Erreurs socket       : ${socketErrors}${lastSocketError ? ` (dernière: ${lastSocketError})` : ""}`);
}

await main();