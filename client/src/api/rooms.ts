import client from "./client";

export const RoomVisibility = { Public: 0, Private: 1 } as const;

export const RoomStatus = {
  Waiting: 0,
  InProgress: 1,
  Finished: 2,
  Cancelled: 3,
} as const;

export interface Room {
  id: number;
  quizId: number;
  joinCode: string;
  visibility: number;
  status: number;
  quizTitle: string;
  participantCount: number;
}

export interface CreateRoomRequest {
  quizId: number;
  roomVisibility: number;
}

export interface JoinRoomRequest {
  joinCode: string;
  displayName: string;
}

export interface JoinedRoom {
  room: Room;
  participantId: number;
  displayName: string;
  playerToken: string;
}

export const roomApi = {
  getPublic: () => client.get<Room[]>("/rooms/public"),

  getMine: () => client.get<Room[]>("/rooms/mine"),

  getByCode: (code: string) =>
    client.get<Room>(`/rooms/${encodeURIComponent(code)}`),

  create: (request: CreateRoomRequest) =>
    client.post<Room>("/rooms", request),

  join: (request: JoinRoomRequest) =>
    client.post<JoinedRoom>("/rooms/join", request),
};
