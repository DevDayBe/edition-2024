import {inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Sheep} from "../models/sheep";
import {Post} from "../models/post";

@Injectable({
  providedIn: 'root'
})
export class SheepService {

  private http = inject(HttpClient);

  list(){
    return this.http.get<Sheep[]>('/assets/data/sheeps.json')
  }

  getById(id:number){
    return this.http.get<Sheep>(`/assets/data/sheeps/${id}.json`);
  }

  getPosts(id:number){
    return this.http.get<Post[]>(`/assets/data/posts/${id}.json`);
  }


}
