export type SessionInvalidReason = 'expired' | 'invalid';

type EnsureSessionHandler = () => Promise<boolean>;
type UnauthorizedHandler = (reason: SessionInvalidReason) => void;

let ensureSessionHandler: EnsureSessionHandler = async () => true;
let unauthorizedHandler: UnauthorizedHandler = () => {};

export function configureApiAuthSession(options: {
  ensureSession: EnsureSessionHandler;
  onUnauthorized: UnauthorizedHandler;
}): void {
  ensureSessionHandler = options.ensureSession;
  unauthorizedHandler = options.onUnauthorized;
}

export async function ensureApiSession(): Promise<boolean> {
  return ensureSessionHandler();
}

export function notifyUnauthorizedSession(reason: SessionInvalidReason): void {
  unauthorizedHandler(reason);
}
