import client from "./client";

export interface Room {
  id: number;
  quizId: number;
  joinCode: string;
  visibility: number;
  status: number;
  participantCount: number;
}

export interface CreateRoomRequest {
  quizId: number;
  roomVisibility: number;
}

export const roomApi = {
  getPublic: () => client.get<Room[]>("/rooms/public"),

  getMine: () => client.get<Room[]>("/rooms/mine"),

  create: (request: CreateRoomRequest) =>
    client.post<Room>("/rooms", request),
};