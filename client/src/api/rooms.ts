import client from "./client";

export interface Room {
  id: number;
  quizId: number;
  joinCode: string;
  visibility: number;
  status: number;
  participantCount: number;
  quizTitle: string;
}

export interface CreateRoomRequest {
  quizId: number;
  roomVisibility: number;
}

export interface JoinRoomRequest {
  joinCode: string;
  displayName: string;
}

export const roomApi = {
  getPublic: () => client.get<Room[]>("/rooms/public"),

  getMine: () => client.get<Room[]>("/rooms/mine"),

  getByCode: (code: string) =>
    client.get<Room>(`/rooms/${encodeURIComponent(code)}`),

  create: (request: CreateRoomRequest) =>
    client.post<Room>("/rooms", request),

  join: (request: JoinRoomRequest) =>
    client.post<Room>("/rooms/join", request),
};